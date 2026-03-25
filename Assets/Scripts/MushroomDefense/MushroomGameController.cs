using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MushroomDefense
{
    public class MushroomGameController : MonoBehaviour
    {
        private const int GridWidth = 4;
        private const int GridHeight = 4;
        private const float GridWidthScreenFraction = 0.6f;
        private const float WaveDelaySeconds = 60f;
        private const int MaxWaves = 10;

        private const int SpawnCost = 15;
        private const int HealCost = 12;

        private readonly int[] _upgradeCostByLevel = { 0, 20, 40, 70 };

        private readonly float[] _mushroomMaxHp = { 30f, 50f, 80f, 120f };
        private readonly float[] _mushroomDamage = { 3f, 6f, 10f, 15f };
        private readonly float[] _mushroomAttackInterval = { 1.0f, 0.85f, 0.7f, 0.55f };
        private readonly float[] _mushroomAttackRange = { 2.2f, 2.6f, 3.0f, 3.4f };
        private readonly int[] _mushroomCurrencyAmount = { 4, 7, 11, 16 };
        private readonly float[] _mushroomCurrencyInterval = { 4.5f, 4f, 3.5f, 3f };

        private readonly float[] _enemyMaxHp = { 20f, 40f, 70f };
        private readonly float[] _enemyDamage = { 4f, 7f, 11f };
        private readonly float[] _enemyAttackInterval = { 1.2f, 1.0f, 0.8f };
        private readonly float[] _enemyMoveSpeed = { 1.4f, 1.1f, 0.8f };
        private readonly int[] _enemyReward = { 8, 14, 24 };

        private readonly List<MushroomData> _mushrooms = new List<MushroomData>();
        private readonly List<EnemyData> _enemies = new List<EnemyData>();

        private readonly Dictionary<Collider2D, CellData> _cellByCollider = new Dictionary<Collider2D, CellData>();
        private readonly Dictionary<Collider2D, MushroomData> _mushroomByCollider = new Dictionary<Collider2D, MushroomData>();

        private Camera _mainCamera;

        private Sprite _tileSprite;
        private Sprite _backgroundSprite;
        private Sprite _buttonSprite;
        private Sprite[] _mushroomSprites;
        private Sprite[] _tickSprites;
        private Sprite[] _mosquitoSprites;
        private Sprite[] _hareSprites;
        private Sprite _fallbackSprite;

        private int _currency = 50;
        private int _currentWave;
        private float _timeToNextWave = WaveDelaySeconds;
        private bool _waveInProgress;
        private bool _gameEnded;
        private float _cellSize = 1.8f;
        private Vector2 _gridOrigin;
        private float _gridWorldWidth;
        private float _gridWorldHeight;

        private enum SelectionType { None, EmptyCell, Mushroom }
        private SelectionType _selectionType;
        private CellData _selectedCell;
        private MushroomData _selectedMushroom;

        private Canvas _canvas;
        private Text _currencyText;
        private Text _waveText;
        private Text _nextWaveText;
        private Text _warningText;
        private Button _spawnButton;
        private Button _upgradeButton;
        private Button _healButton;
        private GameObject _endPanel;
        private Text _endTitle;

        private void Awake()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                _mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 6f;
            _mainCamera.transform.position = new Vector3(0f, 0f, -10f);

            EnsureEventSystem();
            LoadSprites();
            BuildWorld();
            BuildUi();
            RefreshUi();
        }

        private void Update()
        {
            if (_gameEnded) return;

            HandlePointerInput();
            TickMushrooms(Time.deltaTime);
            TickEnemies(Time.deltaTime);
            TickWave(Time.deltaTime);

            if (_waveInProgress && _mushrooms.Count == 0)
            {
                EndGame(false);
                return;
            }

            RefreshUi();
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private void LoadSprites()
        {
            _fallbackSprite = CreateSolidSprite();
            _tileSprite = LoadEditorSprite("Assets/Art/tile.png") ?? _fallbackSprite;
            _backgroundSprite = LoadEditorSprite("Assets/Art/bkg.png") ?? _fallbackSprite;
            _buttonSprite = LoadEditorSprite("Assets/Art/button_blue.png");

            _mushroomSprites = new[]
            {
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_1.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_2.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_3.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_4.png") ?? _fallbackSprite
            };
            _tickSprites = new[]
            {
                LoadEditorSprite("Assets/Art/Ticks/tick_1.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Ticks/tick_2.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Ticks/tick_3.png") ?? _fallbackSprite
            };
            _mosquitoSprites = new[]
            {
                LoadEditorSprite("Assets/Art/Mosquitoes/mosquitoe_1.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mosquitoes/mosquitoe_2.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mosquitoes/mosquitoe_3.png") ?? _fallbackSprite
            };
            _hareSprites = new[]
            {
                LoadEditorSprite("Assets/Art/Hares/hare_1.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Hares/hare_2.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Hares/hare_3.png") ?? _fallbackSprite
            };
        }

        private void BuildWorld()
        {
            var viewportHeight = _mainCamera.orthographicSize * 2f;
            var viewportWidth = viewportHeight * _mainCamera.aspect;

            var background = new GameObject("Background");
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = _backgroundSprite;
            backgroundRenderer.sortingOrder = -10;
            backgroundRenderer.color = _backgroundSprite == _fallbackSprite ? new Color(0.18f, 0.26f, 0.19f) : Color.white;
            var cameraPos = _mainCamera.transform.position;
            background.transform.position = new Vector3(cameraPos.x, cameraPos.y, 0f);
            background.transform.localScale = GetBackgroundScale(viewportWidth, viewportHeight);

            _cellSize = (viewportWidth * GridWidthScreenFraction) / GridWidth;

            _gridWorldWidth = _cellSize * GridWidth;
            _gridWorldHeight = _cellSize * GridHeight;
            var gridCenterY = -viewportHeight * 0.12f;
            _gridOrigin = new Vector2(
                -_gridWorldWidth * 0.5f + _cellSize * 0.5f,
                gridCenterY - _gridWorldHeight * 0.5f + _cellSize * 0.5f);
            for (var y = 0; y < GridHeight; y++)
            {
                for (var x = 0; x < GridWidth; x++)
                {
                    var cellObject = new GameObject($"Tile_{x}_{y}");
                    var renderer = cellObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = _tileSprite;
                    renderer.sortingOrder = 0;
                    renderer.color = Color.white;
                    cellObject.transform.localScale = GetTileScale();

                    var collider = cellObject.AddComponent<BoxCollider2D>();
                    var spriteSize = _tileSprite.bounds.size;
                    collider.size = spriteSize.x > Mathf.Epsilon && spriteSize.y > Mathf.Epsilon
                        ? new Vector2(spriteSize.x, spriteSize.y)
                        : Vector2.one;
                    collider.offset = _tileSprite.bounds.center;

                    var pos = _gridOrigin + new Vector2(x * _cellSize, y * _cellSize);
                    cellObject.transform.position = new Vector3(pos.x, pos.y, 0f);

                    var cell = new CellData { WorldPosition = pos, Renderer = renderer, Collider = collider };
                    _cellByCollider[collider] = cell;
                }
            }
        }

        private Vector3 GetTileScale()
        {
            var spriteSize = _tileSprite.bounds.size;
            if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
            {
                return new Vector3(_cellSize, _cellSize, 1f);
            }

            var scaleX = _cellSize / spriteSize.x;
            var scaleY = _cellSize / spriteSize.y;
            return new Vector3(scaleX, scaleY, 1f);
        }

        private Vector3 GetBackgroundScale(float viewportWidth, float viewportHeight)
        {
            var spriteSize = _backgroundSprite.bounds.size;
            if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
            {
                return new Vector3(viewportWidth, viewportHeight, 1f);
            }

            var scale = Mathf.Max(viewportWidth / spriteSize.x, viewportHeight / spriteSize.y);
            return new Vector3(scale, scale, 1f);
        }

        private void OnDrawGizmos()
        {
            if (_cellSize <= 0f || _gridWorldWidth <= 0f || _gridWorldHeight <= 0f)
            {
                return;
            }

            var min = _gridOrigin - new Vector2(_cellSize * 0.5f, _cellSize * 0.5f);
            var max = min + new Vector2(_gridWorldWidth, _gridWorldHeight);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((min + max) * 0.5f, new Vector3(_gridWorldWidth, _gridWorldHeight, 0f));

            Gizmos.color = Color.yellow;
            for (var x = 0; x <= GridWidth; x++)
            {
                var worldX = min.x + x * _cellSize;
                Gizmos.DrawLine(new Vector3(worldX, min.y, 0f), new Vector3(worldX, max.y, 0f));
            }

            for (var y = 0; y <= GridHeight; y++)
            {
                var worldY = min.y + y * _cellSize;
                Gizmos.DrawLine(new Vector3(min.x, worldY, 0f), new Vector3(max.x, worldY, 0f));
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("HUD");
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            _currencyText = CreateText("Currency", new Vector2(1f, 1f), new Vector2(-20f, -20f), TextAnchor.UpperRight, 28);
            _waveText = CreateText("Wave", new Vector2(0f, 1f), new Vector2(20f, -20f), TextAnchor.UpperLeft, 28);
            _nextWaveText = CreateText("NextWave", new Vector2(0.5f, 1f), new Vector2(0f, -20f), TextAnchor.UpperCenter, 26);
            _warningText = CreateText("Warning", new Vector2(0.5f, 0.8f), new Vector2(0f, 0f), TextAnchor.MiddleCenter, 34);
            _warningText.color = new Color(1f, 0.3f, 0.3f, 0f);

            _spawnButton = CreateButton("Spawn Mushroom", new Vector2(1f, 0f), new Vector2(-150f, 120f), SpawnMushroomOnSelectedCell);
            _upgradeButton = CreateButton("Upgrade", new Vector2(1f, 0f), new Vector2(-150f, 70f), UpgradeSelectedMushroom);
            _healButton = CreateButton("Heal", new Vector2(1f, 0f), new Vector2(-150f, 20f), HealSelectedMushroom);

            _endPanel = new GameObject("EndPanel");
            _endPanel.transform.SetParent(_canvas.transform, false);
            var panelRect = _endPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = _endPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.8f);

            _endTitle = CreateText("EndTitle", new Vector2(0.5f, 0.65f), Vector2.zero, TextAnchor.MiddleCenter, 40, _endPanel.transform);
            var restartButton = CreateButton("Restart", new Vector2(0.5f, 0.32f), Vector2.zero, RestartGame, _endPanel.transform);
            restartButton.GetComponentInChildren<Text>().fontSize = 30;
            _endPanel.SetActive(false);
        }
        private void HandlePointerInput()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            var world = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));

            if (hit != null && _mushroomByCollider.TryGetValue(hit, out var mushroom))
            {
                SelectMushroom(mushroom);
                HarvestMushroomClick(mushroom);
                return;
            }

            if (hit != null && _cellByCollider.TryGetValue(hit, out var cell))
            {
                if (cell.Occupant == null) SelectCell(cell);
                else
                {
                    SelectMushroom(cell.Occupant);
                    HarvestMushroomClick(cell.Occupant);
                }
                return;
            }

            ClearSelection();
        }

        private void TickMushrooms(float deltaTime)
        {
            foreach (var mushroom in _mushrooms.ToArray())
            {
                mushroom.AttackCooldown -= deltaTime;
                mushroom.CurrencyTimer += deltaTime;

                var interval = GetMushroomCurrencyInterval(mushroom.Level);
                if (mushroom.CurrencyTimer >= interval)
                {
                    mushroom.CurrencyTimer -= interval;
                    AddCurrency(GetMushroomCurrencyAmount(mushroom.Level));
                }

                if (_enemies.Count == 0)
                {
                    UpdateMushroomBars(mushroom);
                    continue;
                }

                if (mushroom.AttackCooldown > 0f)
                {
                    UpdateMushroomBars(mushroom);
                    continue;
                }

                var target = FindClosestEnemy(mushroom.WorldPosition, GetMushroomAttackRange(mushroom.Level));
                if (target != null)
                {
                    target.Health -= GetMushroomDamage(mushroom.Level);
                    mushroom.AttackCooldown = GetMushroomAttackInterval(mushroom.Level);
                    if (target.Health <= 0f) KillEnemy(target);
                }

                UpdateMushroomBars(mushroom);
            }
        }

        private void TickEnemies(float deltaTime)
        {
            foreach (var enemy in _enemies.ToArray())
            {
                if (enemy.Target == null || enemy.Target.Health <= 0f) enemy.Target = FindRandomMushroom();
                if (enemy.Target == null) continue;

                enemy.AttackCooldown -= deltaTime;
                var toTarget = enemy.Target.WorldPosition - enemy.WorldPosition;
                var distance = toTarget.magnitude;

                if (distance > 0.2f)
                {
                    var direction = toTarget.normalized;
                    enemy.WorldPosition += direction * GetEnemyMoveSpeed(enemy.Level) * deltaTime;
                    enemy.Renderer.transform.position = new Vector3(enemy.WorldPosition.x, enemy.WorldPosition.y, 0f);
                    enemy.HealthBarRoot.transform.position = enemy.WorldPosition + new Vector2(0f, 0.8f);
                }
                else if (enemy.AttackCooldown <= 0f)
                {
                    enemy.Target.Health -= GetEnemyDamage(enemy.Level);
                    enemy.AttackCooldown = GetEnemyAttackInterval(enemy.Level);
                    if (enemy.Target.Health <= 0f) KillMushroom(enemy.Target);
                }

                UpdateEnemyBar(enemy);
            }
        }

        private void TickWave(float deltaTime)
        {
            if (_waveInProgress)
            {
                if (_enemies.Count == 0)
                {
                    _waveInProgress = false;
                    if (_currentWave >= MaxWaves)
                    {
                        EndGame(true);
                        return;
                    }
                    _timeToNextWave = WaveDelaySeconds;
                }
                return;
            }

            if (_currentWave >= MaxWaves) return;

            _timeToNextWave -= deltaTime;
            if (_timeToNextWave <= 0f) StartWave(_currentWave + 1);
        }

        private void StartWave(int waveIndex)
        {
            _currentWave = waveIndex;
            _waveInProgress = true;

            var enemyCount = 3 + waveIndex * 2;
            var maxLevel = Mathf.Clamp(1 + (waveIndex - 1) / 4, 1, 3);

            for (var i = 0; i < enemyCount; i++)
            {
                var level = Random.Range(1, maxLevel + 1);
                SpawnEnemy(level);
            }

            ClearSelection();
        }

        private void SpawnEnemy(int level)
        {
            var spriteSetRoll = Random.value;
            Sprite sprite;
            if (spriteSetRoll < 0.34f) sprite = _tickSprites[level - 1];
            else if (spriteSetRoll < 0.67f) sprite = _mosquitoSprites[level - 1];
            else sprite = _hareSprites[level - 1];

            var enemyObject = new GameObject($"Enemy_L{level}");
            var renderer = enemyObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;
            renderer.color = sprite == _fallbackSprite ? new Color(0.85f, 0.25f, 0.25f) : Color.white;

            var position = GetEnemySpawnPosition();
            enemyObject.transform.position = position;
            enemyObject.transform.localScale = Vector3.one * 1.2f;

            var healthBarRoot = new GameObject("EnemyHP");
            var hpBar = CreateBar(healthBarRoot.transform, Color.red, 0.18f, 0.08f, 6);
            healthBarRoot.transform.position = (Vector2)position + new Vector2(0f, 0.8f);

            var enemy = new EnemyData
            {
                Level = level,
                Health = GetEnemyMaxHp(level),
                WorldPosition = position,
                Renderer = renderer,
                HealthBarRoot = healthBarRoot,
                HealthBar = hpBar
            };

            _enemies.Add(enemy);
            enemy.Target = FindRandomMushroom();
            UpdateEnemyBar(enemy);
        }

        private Vector2 GetEnemySpawnPosition()
        {
            const float topY = 5.4f;
            const float bottomY = -5.4f;
            const float leftX = -8.8f;
            const float rightX = 8.8f;

            var roll = Random.value;
            if (roll < 0.7f) return new Vector2(Random.Range(leftX, rightX), topY);

            var sideRoll = Random.value;
            if (sideRoll < 0.33f) return new Vector2(leftX, Random.Range(bottomY, topY));
            if (sideRoll < 0.66f) return new Vector2(rightX, Random.Range(bottomY, topY));
            return new Vector2(Random.Range(leftX, rightX), bottomY);
        }
        private void SpawnMushroomOnSelectedCell()
        {
            if (_selectedCell == null) return;
            if (!TrySpendCurrency(SpawnCost)) return;

            var mushroomObject = new GameObject("Mushroom_L1");
            var renderer = mushroomObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _mushroomSprites[0];
            renderer.sortingOrder = 3;
            renderer.color = renderer.sprite == _fallbackSprite ? new Color(0.8f, 0.75f, 0.2f) : Color.white;
            mushroomObject.transform.position = new Vector3(_selectedCell.WorldPosition.x, _selectedCell.WorldPosition.y, 0f);
            mushroomObject.transform.localScale = Vector3.one * 1.2f;

            var collider = mushroomObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.8f;

            var barsRoot = new GameObject("MushroomBars");
            barsRoot.transform.position = _selectedCell.WorldPosition + new Vector2(0f, -0.85f);
            var currencyBar = CreateBar(barsRoot.transform, new Color(1f, 0.55f, 0.15f), 0.09f, 0.07f, 5);
            var hpBar = CreateBar(barsRoot.transform, Color.red, -0.09f, 0.07f, 5);

            var mushroom = new MushroomData
            {
                Level = 1,
                Health = GetMushroomMaxHp(1),
                WorldPosition = _selectedCell.WorldPosition,
                Renderer = renderer,
                Collider = collider,
                Cell = _selectedCell,
                BarsRoot = barsRoot,
                CurrencyBar = currencyBar,
                HealthBar = hpBar
            };

            _selectedCell.Occupant = mushroom;
            _mushrooms.Add(mushroom);
            _mushroomByCollider[collider] = mushroom;

            SelectMushroom(mushroom);
            UpdateMushroomBars(mushroom);
        }

        private void UpgradeSelectedMushroom()
        {
            if (_selectedMushroom == null || _waveInProgress) return;
            if (_selectedMushroom.Level >= 4) return;

            var price = _upgradeCostByLevel[_selectedMushroom.Level];
            if (!TrySpendCurrency(price)) return;

            _selectedMushroom.Level++;
            _selectedMushroom.Renderer.sprite = _mushroomSprites[_selectedMushroom.Level - 1];
            _selectedMushroom.Health = Mathf.Min(_selectedMushroom.Health + 10f, GetMushroomMaxHp(_selectedMushroom.Level));
            _selectedMushroom.Renderer.gameObject.name = $"Mushroom_L{_selectedMushroom.Level}";
            UpdateMushroomBars(_selectedMushroom);
        }

        private void HealSelectedMushroom()
        {
            if (_selectedMushroom == null || _waveInProgress) return;
            if (_selectedMushroom.Health >= GetMushroomMaxHp(_selectedMushroom.Level) - 0.01f) return;
            if (!TrySpendCurrency(HealCost)) return;

            _selectedMushroom.Health = GetMushroomMaxHp(_selectedMushroom.Level);
            UpdateMushroomBars(_selectedMushroom);
        }

        private void HarvestMushroomClick(MushroomData mushroom) => AddCurrency(GetMushroomCurrencyAmount(mushroom.Level));
        private void AddCurrency(int amount) => _currency += amount;

        private bool TrySpendCurrency(int amount)
        {
            if (_currency < amount)
            {
                ShowWarning("Not enough money");
                return false;
            }

            _currency -= amount;
            return true;
        }

        private void ShowWarning(string text)
        {
            StopCoroutine(nameof(FadeWarning));
            _warningText.text = text;
            StartCoroutine(FadeWarning());
        }

        private IEnumerator FadeWarning()
        {
            var color = _warningText.color;
            color.a = 1f;
            _warningText.color = color;
            yield return new WaitForSeconds(0.7f);

            var t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(1f, 0f, t / 0.7f);
                _warningText.color = color;
                yield return null;
            }

            color.a = 0f;
            _warningText.color = color;
        }

        private void KillEnemy(EnemyData enemy)
        {
            AddCurrency(_enemyReward[enemy.Level - 1]);
            _enemies.Remove(enemy);
            Destroy(enemy.Renderer.gameObject);
            Destroy(enemy.HealthBarRoot);
        }

        private void KillMushroom(MushroomData mushroom)
        {
            if (_selectedMushroom == mushroom) ClearSelection();

            _mushrooms.Remove(mushroom);
            _mushroomByCollider.Remove(mushroom.Collider);
            mushroom.Cell.Occupant = null;
            Destroy(mushroom.Renderer.gameObject);
            Destroy(mushroom.BarsRoot);
        }

        private EnemyData FindClosestEnemy(Vector2 origin, float maxDistance)
        {
            EnemyData best = null;
            var bestDistance = float.MaxValue;
            foreach (var enemy in _enemies)
            {
                var distance = Vector2.Distance(origin, enemy.WorldPosition);
                if (distance > maxDistance || distance >= bestDistance) continue;
                bestDistance = distance;
                best = enemy;
            }
            return best;
        }

        private MushroomData FindRandomMushroom()
        {
            if (_mushrooms.Count == 0) return null;
            return _mushrooms[Random.Range(0, _mushrooms.Count)];
        }

        private void SelectCell(CellData cell)
        {
            ClearSelection();
            _selectionType = SelectionType.EmptyCell;
            _selectedCell = cell;
            cell.Renderer.color = new Color(1f, 1f, 0.55f, 1f);
        }

        private void SelectMushroom(MushroomData mushroom)
        {
            ClearSelection();
            _selectionType = SelectionType.Mushroom;
            _selectedMushroom = mushroom;
            mushroom.Renderer.color = new Color(1f, 0.95f, 0.55f, 1f);
        }

        private void ClearSelection()
        {
            if (_selectedCell != null) _selectedCell.Renderer.color = Color.white;
            if (_selectedMushroom != null) _selectedMushroom.Renderer.color = Color.white;
            _selectionType = SelectionType.None;
            _selectedCell = null;
            _selectedMushroom = null;
        }

        private void EndGame(bool win)
        {
            _gameEnded = true;
            _endPanel.SetActive(true);
            _endTitle.text = win ? "You win the game!" : "You Failed!";
        }

        private void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        private void RefreshUi()
        {
            _currencyText.text = $"{_currency}";
            _waveText.text = $"Wave: {_currentWave}/{MaxWaves}";
            _nextWaveText.text = _waveInProgress ? $"Enemies alive: {_enemies.Count}" : $"Next wave in: {Mathf.CeilToInt(Mathf.Max(0f, _timeToNextWave))}s";

            _spawnButton.gameObject.SetActive(_selectionType == SelectionType.EmptyCell);
            _upgradeButton.gameObject.SetActive(_selectionType == SelectionType.Mushroom && !_waveInProgress && _selectedMushroom != null && _selectedMushroom.Level < 4);
            _healButton.gameObject.SetActive(_selectionType == SelectionType.Mushroom && !_waveInProgress && _selectedMushroom != null && _selectedMushroom.Health < GetMushroomMaxHp(_selectedMushroom.Level));
        }

        private Text CreateText(string name, Vector2 anchor, Vector2 offset, TextAnchor align, int fontSize, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent == null ? _canvas.transform : parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(520f, 70f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = align;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateButton(string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction onClick, Transform parent = null)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent == null ? _canvas.transform : parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(220f, 42f);

            var image = go.AddComponent<Image>();
            image.color = Color.white;
            if (_buttonSprite != null)
            {
                image.sprite = _buttonSprite;
                image.type = Image.Type.Sliced;
            }

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var text = CreateText("Label", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleCenter, 24, go.transform);
            text.text = label;
            text.rectTransform.sizeDelta = rect.sizeDelta;
            return button;
        }
        private SpriteRenderer CreateBar(Transform parent, Color color, float localY, float height, int sortingOrder)
        {
            var bar = new GameObject("Bar");
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = new Vector3(0f, localY, 0f);
            bar.transform.localScale = new Vector3(1f, height, 1f);

            var renderer = bar.AddComponent<SpriteRenderer>();
            renderer.sprite = _fallbackSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void UpdateMushroomBars(MushroomData mushroom)
        {
            mushroom.BarsRoot.transform.position = mushroom.WorldPosition + new Vector2(0f, -0.85f);
            var hpRatio = Mathf.Clamp01(mushroom.Health / GetMushroomMaxHp(mushroom.Level));
            var currencyRatio = Mathf.Clamp01(mushroom.CurrencyTimer / GetMushroomCurrencyInterval(mushroom.Level));
            mushroom.HealthBar.transform.localScale = new Vector3(Mathf.Max(0.02f, hpRatio), mushroom.HealthBar.transform.localScale.y, 1f);
            mushroom.CurrencyBar.transform.localScale = new Vector3(Mathf.Max(0.02f, currencyRatio), mushroom.CurrencyBar.transform.localScale.y, 1f);
        }

        private void UpdateEnemyBar(EnemyData enemy)
        {
            var hpRatio = Mathf.Clamp01(enemy.Health / GetEnemyMaxHp(enemy.Level));
            enemy.HealthBar.transform.localScale = new Vector3(Mathf.Max(0.02f, hpRatio), enemy.HealthBar.transform.localScale.y, 1f);
        }

        private float GetMushroomMaxHp(int level) => _mushroomMaxHp[level - 1];
        private float GetMushroomDamage(int level) => _mushroomDamage[level - 1];
        private float GetMushroomAttackInterval(int level) => _mushroomAttackInterval[level - 1];
        private float GetMushroomAttackRange(int level) => _mushroomAttackRange[level - 1];
        private int GetMushroomCurrencyAmount(int level) => _mushroomCurrencyAmount[level - 1];
        private float GetMushroomCurrencyInterval(int level) => _mushroomCurrencyInterval[level - 1];

        private float GetEnemyMaxHp(int level) => _enemyMaxHp[level - 1];
        private float GetEnemyDamage(int level) => _enemyDamage[level - 1];
        private float GetEnemyAttackInterval(int level) => _enemyAttackInterval[level - 1];
        private float GetEnemyMoveSpeed(int level) => _enemyMoveSpeed[level - 1];

        private static Sprite CreateSolidSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Sprite LoadEditorSprite(string path)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
#else
            _ = path;
            return null;
#endif
        }

        private sealed class CellData
        {
            public Vector2 WorldPosition;
            public SpriteRenderer Renderer;
            public Collider2D Collider;
            public MushroomData Occupant;
        }

        private sealed class MushroomData
        {
            public int Level;
            public float Health;
            public float AttackCooldown;
            public float CurrencyTimer;
            public Vector2 WorldPosition;
            public SpriteRenderer Renderer;
            public Collider2D Collider;
            public CellData Cell;
            public GameObject BarsRoot;
            public SpriteRenderer CurrencyBar;
            public SpriteRenderer HealthBar;
        }

        private sealed class EnemyData
        {
            public int Level;
            public float Health;
            public float AttackCooldown;
            public Vector2 WorldPosition;
            public SpriteRenderer Renderer;
            public MushroomData Target;
            public GameObject HealthBarRoot;
            public SpriteRenderer HealthBar;
        }
    }
}
