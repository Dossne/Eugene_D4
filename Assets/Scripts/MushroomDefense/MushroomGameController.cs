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
        private const float MushroomScale = 0.53f;
        private const float TileGapInTileWidths = 0.1f;
        // 0 = bottom edge of tile, 1 = top edge of tile
        private const float MushroomVisualYInCell = 0.3f;
        private const float ActionButtonWorldOffsetXInCell = 0.3f;
        private const float ActionButtonWorldOffsetYInCell = 0.35f;
        private const float ActionButtonVerticalGap = -6f;
        private const float ButtonWidth = 200f;
        private const float ButtonHeight = 100f;
        private const float SpawnIconScaleFactor = 0.75f;
        private const float CoinIconScaleFactor = 1.2f;
        private const float LeftSideWidthRatio = 0.45f;
        // 0 = bottom edge of button, 1 = top edge of button
        private const float LeftIconVerticalNormalized = 0.55f;
        private const float MushroomBarWidth = 0.8f;
        private const float MushroomCurrencyBarHeight = 0.10f;
        private const float MushroomHealthBarHeight = 0.16f;
        private const float MushroomBarOutlineWidthPadding = 0.06f;
        private const float MushroomBarOutlineHeightPadding = 0.04f;
        private const float MushroomBarsDividerHeight = 0.025f;
        private static readonly Vector3 MushroomCurrencyCompressScale = new Vector3(1.08f, 0.9f, 1f);
        private static readonly Vector3 MushroomCurrencyBounceTopScale = new Vector3(0.94f, 1.08f, 1f);
        private const float MushroomCurrencyShakeDuration = 1f;
        private const float MushroomCurrencyShakeFrequency = 11f;
        private const float MushroomCurrencyShakeStrength = 0.02f;
        private const float MushroomCurrencyBounceUpDuration = 0.18f;
        private const float MushroomCurrencyBounceDownDuration = 0.2f;
        private const float MushroomIdleDuration = 0.62f;
        private const float MushroomIdleSafetyBeforeCurrency = 0.08f;
        private const float MushroomIdleDelayAfterCurrency = 0.3f;
        private const float MushroomIdleFlipToggleInterval = 0.2f;
        private const int MushroomIdleModeNone = 0;
        private const int MushroomIdleModeJump = 1;
        private const int MushroomIdleModeRotate = 2;
        private const int MushroomIdleModeSpin = 3;
        private const int MushroomIdleModeBounce = 4;
        private static readonly Vector3 MushroomIdleBounceScale = new Vector3(1.04f, 0.95f, 1f);
        private const float MushroomIdleBounceDuration = 0.28f;
        private const float HudPanelMarginX = 20f;
        private const float HudPanelMarginY = -20f;
        private const float CurrencyPanelWidth = 170;
        private const float CurrencyPanelHeight = 90f;
        private const float CurrencyPanelContentScale = 1f;
        private const float CurrencyIconTextSpacing = 14f;
        private const float CurrencyIconSizeFactor = 0.62f;
        private const float CurrencyPopupStartOffsetY = 0.45f;
        private const float CurrencyPopupIconTextSpacing = 7f;
        private const float CurrencyPopupRiseHeight = 1.1f;
        private const float WavePanelWidth = 270f;
        private const float WavePanelHeight = 90f;
        private const float WavePanelTextScale = 0.9f;
        private const int CheatCurrencyBonus = 1000;
        private const KeyCode CheatCurrencyKey = KeyCode.F8;
        private const KeyCode CheatNextWaveKey = KeyCode.W;

        private const int SpawnCost = 15;
        private const int HealCost = 12;

        private readonly int[] _upgradeCostByLevel = { 0, 20, 40, 70 };

        private readonly float[] _mushroomMaxHp = { 30f, 50f, 80f, 120f };
        private readonly float[] _mushroomDamage = { 3f, 6f, 10f, 15f };
        private readonly float[] _mushroomAttackInterval = { 1.0f, 0.85f, 0.7f, 0.55f };
        private readonly float[] _mushroomAttackRange = { 2.2f, 2.6f, 3.0f, 3.4f };
        private readonly int[] _mushroomCurrencyAmount = { 4, 7, 11, 16 };
        private readonly float[] _mushroomCurrencyInterval = { 4.5f, 4f, 3.5f, 3f };
        private readonly float[] _mushroomBarsPivotYOffset = { 0.8f, 1.05f, 1.3f, 1.55f };
        private readonly float[] _mushroomIdleMinDelay = { 1.9f, 2.8f, 2.85f, 2.45f };
        private readonly float[] _mushroomIdleMaxDelay = { 3.0f, 4.0f, 3.35f, 2.9f };
        private readonly float[] _mushroomIdleIntensity = { 1f, 0.72f, 0.45f, 0.32f };

        private readonly float[] _enemyMaxHp = { 20f, 40f, 70f };
        private readonly float[] _enemyDamage = { 4f, 7f, 11f };
        private readonly float[] _enemyAttackInterval = { 1.2f, 1.0f, 0.8f };
        private readonly float[] _enemyMoveSpeed = { 1.4f, 1.1f, 0.8f };
        private readonly int[] _enemyReward = { 8, 14, 24 };

        private readonly List<MushroomData> _mushrooms = new List<MushroomData>();
        private readonly List<EnemyData> _enemies = new List<EnemyData>();
        private readonly List<CurrencyPopupData> _currencyPopups = new List<CurrencyPopupData>();

        private readonly Dictionary<Collider2D, CellData> _cellByCollider = new Dictionary<Collider2D, CellData>();
        private readonly Dictionary<Collider2D, MushroomData> _mushroomByCollider = new Dictionary<Collider2D, MushroomData>();

        private Camera _mainCamera;

        private Sprite _tileSprite;
        private Sprite _backgroundSprite;
        private Sprite _buttonSprite;
        private Sprite _upgradeButtonSprite;
        private Sprite _coinSprite;
        private Sprite _arrowSprite;
        private Sprite _heartSprite;
        private Sprite _uiBackgroundSprite;
        private Sprite _spawnButtonIconSprite;
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
        private float _cellGap;
        private float _cellStep;
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
        private Text _spawnCostText;
        private Text _upgradeCostText;
        private Text _healCostText;
        private Coroutine _warningFadeCoroutine;
        private bool _isWarningVisible;
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

            HandleCheatInput();
            HandlePointerInput();
            TickMushrooms(Time.deltaTime);
            TickEnemies(Time.deltaTime);
            TickWave(Time.deltaTime);
            TickCurrencyPopups(Time.deltaTime);

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
            _upgradeButtonSprite = LoadEditorSprite("Assets/Art/Upgrade.png") ?? _buttonSprite;
            _coinSprite = LoadEditorSprite("Assets/Art/coin.png");
            _arrowSprite = LoadEditorSprite("Assets/Art/arrow.png");
            _heartSprite = LoadEditorSprite("Assets/Art/heart.png");
            _uiBackgroundSprite = LoadEditorSprite("Assets/Art/ui_bkg.png");
            _spawnButtonIconSprite = LoadEditorSprite("Assets/Art/spawn.png");

            _mushroomSprites = new[]
            {
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_1.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_2.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_3.png") ?? _fallbackSprite,
                LoadEditorSprite("Assets/Art/Mushrooms/mushroom_4.png") ?? _fallbackSprite
            };
            if (_spawnButtonIconSprite == null)
            {
                _spawnButtonIconSprite = _mushroomSprites[0];
            }
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

            var desiredGridWidth = viewportWidth * GridWidthScreenFraction;
            var sizeDivisor = GridWidth + (GridWidth - 1) * TileGapInTileWidths;
            _cellSize = desiredGridWidth / sizeDivisor;
            _cellGap = _cellSize * TileGapInTileWidths;
            _cellStep = _cellSize + _cellGap;

            _gridWorldWidth = _cellSize * GridWidth + _cellGap * (GridWidth - 1);
            _gridWorldHeight = _cellSize * GridHeight + _cellGap * (GridHeight - 1);
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

                    var pos = _gridOrigin + new Vector2(x * _cellStep, y * _cellStep);
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

        private static Vector2 GetCellCenter(CellData cell)
        {
            if (cell != null && cell.Renderer != null)
            {
                return cell.Renderer.transform.position;
            }

            if (cell != null && cell.Collider != null)
            {
                return cell.Collider.bounds.center;
            }

            return cell != null ? cell.WorldPosition : Vector2.zero;
        }

        private Vector2 GetMushroomVisualPosition(CellData cell)
        {
            var center = GetCellCenter(cell);
            var yOffsetFromCenter = (MushroomVisualYInCell - 0.5f) * _cellSize;
            return center + Vector2.up * yOffsetFromCenter;
        }

        private void OnDrawGizmos()
        {
            if (_cellSize <= 0f || _cellStep <= 0f || _gridWorldWidth <= 0f || _gridWorldHeight <= 0f)
            {
                return;
            }

            var min = _gridOrigin - new Vector2(_cellSize * 0.5f, _cellSize * 0.5f);
            var max = min + new Vector2(_gridWorldWidth, _gridWorldHeight);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube((min + max) * 0.5f, new Vector3(_gridWorldWidth, _gridWorldHeight, 0f));

            Gizmos.color = Color.yellow;
            for (var y = 0; y < GridHeight; y++)
            {
                for (var x = 0; x < GridWidth; x++)
                {
                    var center = new Vector2(min.x + _cellSize * 0.5f + x * _cellStep, min.y + _cellSize * 0.5f + y * _cellStep);
                    Gizmos.DrawWireCube(center, new Vector3(_cellSize, _cellSize, 0f));
                }
            }
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("HUD");
            _canvas = canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var beigePanelColor = new Color(0.88f, 0.82f, 0.72f, 0.95f);

            var currencyPanel = CreateHudPanel("CurrencyPanel", new Vector2(1f, 1f), new Vector2(-HudPanelMarginX, HudPanelMarginY), new Vector2(1f, 1f), new Vector2(CurrencyPanelWidth, CurrencyPanelHeight), beigePanelColor);
            BuildCurrencyPanelContent(currencyPanel);

            var wavePanel = CreateHudPanel("WavePanel", new Vector2(0f, 1f), new Vector2(HudPanelMarginX, HudPanelMarginY), new Vector2(0f, 1f), new Vector2(WavePanelWidth, WavePanelHeight), beigePanelColor);
            var waveTextRoot = new GameObject("WaveTextRoot");
            waveTextRoot.transform.SetParent(wavePanel.transform, false);
            var waveTextRootRect = waveTextRoot.AddComponent<RectTransform>();
            waveTextRootRect.anchorMin = Vector2.zero;
            waveTextRootRect.anchorMax = Vector2.one;
            waveTextRootRect.pivot = new Vector2(0.5f, 0.5f);
            waveTextRootRect.offsetMin = Vector2.zero;
            waveTextRootRect.offsetMax = Vector2.zero;
            waveTextRootRect.localScale = Vector3.one * WavePanelTextScale;

            _waveText = CreateText("Wave", new Vector2(0.5f, 0.75f), Vector2.zero, TextAnchor.MiddleLeft, 28, waveTextRoot.transform);
            ConfigureWavePanelTextRect(_waveText, true);
            _waveText.color = new Color(0.17f, 0.14f, 0.09f, 1f);

            _nextWaveText = CreateText("NextWave", new Vector2(0.5f, 0.25f), Vector2.zero, TextAnchor.MiddleLeft, 26, waveTextRoot.transform);
            ConfigureWavePanelTextRect(_nextWaveText, false);
            _nextWaveText.color = new Color(0.17f, 0.14f, 0.09f, 1f);

            _warningText = CreateText("Warning", new Vector2(0.5f, 0.72f), new Vector2(0f, 0f), TextAnchor.MiddleCenter, 40);
            _warningText.fontStyle = FontStyle.Bold;
            var warningOutline = _warningText.gameObject.AddComponent<Outline>();
            warningOutline.effectColor = new Color(0f, 0f, 0f, 0.7f);
            warningOutline.effectDistance = new Vector2(1f, -1f);
            _warningText.color = new Color(1f, 0.08f, 0.08f, 0f);

            _spawnButton = CreateActionButton("Spawn", new Vector2(1f, 0f), new Vector2(-150f, 120f), SpawnMushroomOnSelectedCell, _spawnButtonIconSprite, out _spawnCostText);
            _upgradeButton = CreateActionButton("Upgrade", new Vector2(1f, 0f), new Vector2(-150f, 70f), UpgradeSelectedMushroom, _arrowSprite, out _upgradeCostText, null, _upgradeButtonSprite);
            _healButton = CreateActionButton("Heal", new Vector2(1f, 0f), new Vector2(-150f, 20f), HealSelectedMushroom, _heartSprite, out _healCostText);
            ConfigureActionButtonRect(_spawnButton);
            ConfigureActionButtonRect(_upgradeButton);
            ConfigureActionButtonRect(_healButton);

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

        private GameObject CreateHudPanel(string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 pivot, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(_canvas.transform, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = panel.AddComponent<Image>();
            image.sprite = _uiBackgroundSprite ?? _fallbackSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            return panel;
        }

        private void BuildCurrencyPanelContent(GameObject currencyPanel)
        {
            var contentRoot = new GameObject("CurrencyContentRoot");
            contentRoot.transform.SetParent(currencyPanel.transform, false);

            var rootRect = contentRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localScale = Vector3.one * CurrencyPanelContentScale;

            var icon = new GameObject("CurrencyIcon");
            icon.transform.SetParent(contentRoot.transform, false);
            var iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(1f, 0.5f);
            var iconSize = CurrencyPanelHeight * CurrencyIconSizeFactor;
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
            iconRect.anchoredPosition = new Vector2(-CurrencyIconTextSpacing * 0.5f, 0f);
            var iconImage = icon.AddComponent<Image>();
            iconImage.sprite = _coinSprite ?? _fallbackSprite;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;

            _currencyText = CreateText("CurrencyAmount", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleLeft, 36, contentRoot.transform);
            var amountRect = _currencyText.rectTransform;
            amountRect.anchorMin = new Vector2(0.5f, 0.5f);
            amountRect.anchorMax = new Vector2(0.5f, 0.5f);
            amountRect.pivot = new Vector2(0f, 0.5f);
            amountRect.anchoredPosition = new Vector2(CurrencyIconTextSpacing * 0.5f, 0f);
            amountRect.sizeDelta = new Vector2(CurrencyPanelWidth * 0.6f, CurrencyPanelHeight * 0.85f);
            _currencyText.resizeTextForBestFit = true;
            _currencyText.resizeTextMinSize = 14;
            _currencyText.resizeTextMaxSize = 56;
            _currencyText.color = new Color(0.17f, 0.14f, 0.09f, 1f);
        }

        private static void ConfigureWavePanelTextRect(Text text, bool topRow)
        {
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, topRow ? 0.5f : 0f);
            rect.anchorMax = new Vector2(1f, topRow ? 1f : 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(18f, 4f);
            rect.offsetMax = new Vector2(-18f, -4f);
            rect.anchoredPosition = Vector2.zero;

            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 32;
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

        private void HandleCheatInput()
        {
            if (Input.GetKeyDown(CheatCurrencyKey))
            {
                AddCurrency(CheatCurrencyBonus);
            }

            if (Input.GetKeyDown(CheatNextWaveKey) && !_waveInProgress && _currentWave < MaxWaves)
            {
                _timeToNextWave = 0f;
            }
        }

        private void TickMushrooms(float deltaTime)
        {
            foreach (var mushroom in _mushrooms.ToArray())
            {
                mushroom.AttackCooldown -= deltaTime;
                if (mushroom.IsCurrencyAnimationActive)
                {
                    UpdateMushroomCurrencyAnimation(mushroom, deltaTime);
                }
                else
                {
                    if (mushroom.IsIdleAnimationActive)
                    {
                        UpdateMushroomIdleAnimation(mushroom, deltaTime);
                    }

                    mushroom.CurrencyTimer += deltaTime;
                    var interval = GetMushroomCurrencyInterval(mushroom.Level);
                    var timeUntilCurrency = interval - mushroom.CurrencyTimer;
                    if (mushroom.CurrencyTimer >= interval)
                    {
                        if (mushroom.IsIdleAnimationActive)
                        {
                            StopMushroomIdleAnimation(mushroom);
                        }
                        mushroom.CurrencyTimer -= interval;
                        StartMushroomCurrencyAnimation(mushroom, GetMushroomCurrencyAmount(mushroom.Level));
                    }
                    else if (!mushroom.IsIdleAnimationActive)
                    {
                        mushroom.NextIdleDelay -= deltaTime;
                        if (mushroom.NextIdleDelay <= 0f && timeUntilCurrency > MushroomIdleDuration + MushroomIdleSafetyBeforeCurrency)
                        {
                            StartMushroomIdleAnimation(mushroom);
                        }
                        else if (mushroom.NextIdleDelay <= 0f && timeUntilCurrency <= MushroomIdleDuration + MushroomIdleSafetyBeforeCurrency)
                        {
                            mushroom.QueueIdleAfterCurrency = true;
                        }
                    }
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
            var selectedCellCenter = GetCellCenter(_selectedCell);
            var mushroomVisualPos = GetMushroomVisualPosition(_selectedCell);

            var mushroomObject = new GameObject("Mushroom_L1");
            var renderer = mushroomObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _mushroomSprites[0];
            renderer.sortingOrder = 3;
            renderer.color = renderer.sprite == _fallbackSprite ? new Color(0.8f, 0.75f, 0.2f) : Color.white;
            mushroomObject.transform.localScale = Vector3.one * MushroomScale;
            mushroomObject.transform.position = new Vector3(mushroomVisualPos.x, mushroomVisualPos.y, 0f);

            var collider = mushroomObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one * 0.8f;

            var barsRoot = new GameObject("MushroomBars");
            barsRoot.transform.position = mushroomObject.transform.position + Vector3.up * GetMushroomBarsPivotYOffset(1);
            var currencyBar = CreateMushroomBar(barsRoot.transform, new Color(1f, 0.55f, 0.15f), MushroomCurrencyBarHeight * 0.5f, MushroomCurrencyBarHeight, 5);
            var hpBar = CreateMushroomBar(barsRoot.transform, Color.red, -MushroomHealthBarHeight * 0.5f, MushroomHealthBarHeight, 5);
            var barsDivider = CreateMushroomBarsDivider(barsRoot.transform, 5);

            var mushroom = new MushroomData
            {
                Level = 1,
                Health = GetMushroomMaxHp(1),
                WorldPosition = selectedCellCenter,
                Renderer = renderer,
                Collider = collider,
                Cell = _selectedCell,
                DefaultScale = Vector3.one * MushroomScale,
                BaseVisualPosition = new Vector3(mushroomVisualPos.x, mushroomVisualPos.y, 0f),
                BaseFlipX = false,
                NextIdleDelay = GetRandomMushroomIdleDelay(1),
                BarsRoot = barsRoot,
                CurrencyBar = currencyBar,
                HealthBar = hpBar,
                CurrencyDivider = barsDivider
            };

            _selectedCell.Occupant = mushroom;
            _mushrooms.Add(mushroom);
            _mushroomByCollider[collider] = mushroom;
            _selectedCell.WorldPosition = selectedCellCenter;

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
            var mushroomVisualPos = GetMushroomVisualPosition(_selectedMushroom.Cell);
            _selectedMushroom.BaseVisualPosition = new Vector3(mushroomVisualPos.x, mushroomVisualPos.y, 0f);
            _selectedMushroom.DefaultScale = Vector3.one * MushroomScale;
            _selectedMushroom.Renderer.transform.position = _selectedMushroom.BaseVisualPosition;
            _selectedMushroom.Renderer.transform.rotation = Quaternion.identity;
            _selectedMushroom.Renderer.transform.localScale = _selectedMushroom.DefaultScale;
            _selectedMushroom.Renderer.flipX = _selectedMushroom.BaseFlipX;
            _selectedMushroom.NextIdleDelay = GetRandomMushroomIdleDelay(_selectedMushroom.Level);
            _selectedMushroom.Health = GetMushroomMaxHp(_selectedMushroom.Level);
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

        private void StartMushroomIdleAnimation(MushroomData mushroom)
        {
            if (mushroom == null || mushroom.Renderer == null) return;
            mushroom.IsIdleAnimationActive = true;
            mushroom.IdleAnimationTime = 0f;
            mushroom.QueueIdleAfterCurrency = false;
            mushroom.IdleAnimationMode = PickRandomIdleMode(mushroom.Level);
            mushroom.IdleFlipMode = 0;
            mushroom.IdleFlipTimer = 0f;
            mushroom.IdleFlipBackDone = false;
            mushroom.Renderer.flipX = mushroom.BaseFlipX;

            if (mushroom.IdleAnimationMode == MushroomIdleModeRotate)
            {
                mushroom.BaseFlipX = !mushroom.BaseFlipX;
                mushroom.Renderer.flipX = mushroom.BaseFlipX;
            }
            else if (mushroom.IdleAnimationMode == MushroomIdleModeSpin)
            {
                mushroom.IdleFlipMode = 1;
                mushroom.Renderer.flipX = !mushroom.BaseFlipX;
            }
        }

        private void UpdateMushroomIdleAnimation(MushroomData mushroom, float deltaTime)
        {
            if (mushroom == null || mushroom.Renderer == null) return;

            mushroom.IdleAnimationTime += deltaTime;
            var t = Mathf.Clamp01(mushroom.IdleAnimationTime / MushroomIdleDuration);
            var intensity = GetMushroomIdleIntensity(mushroom.Level);
            var defaultScale = mushroom.DefaultScale;
            var isJumpMode = mushroom.IdleAnimationMode == MushroomIdleModeJump;
            var isSpinMode = mushroom.IdleAnimationMode == MushroomIdleModeSpin;
            var isBounceMode = mushroom.IdleAnimationMode == MushroomIdleModeBounce;

            var jumpOffset = 0f;
            var scale = defaultScale;
            if (isJumpMode)
            {
                var squashScale = Vector3.Scale(defaultScale, new Vector3(1f + 0.08f * intensity, 1f - 0.09f * intensity, 1f));
                var stretchScale = Vector3.Scale(defaultScale, new Vector3(1f - 0.06f * intensity, 1f + 0.07f * intensity, 1f));
                if (t < 0.35f)
                {
                    var phaseT = Mathf.Clamp01(t / 0.35f);
                    scale = Vector3.Lerp(defaultScale, squashScale, Mathf.SmoothStep(0f, 1f, phaseT));
                }
                else if (t < 0.68f)
                {
                    var phaseT = Mathf.Clamp01((t - 0.35f) / 0.33f);
                    scale = Vector3.Lerp(squashScale, stretchScale, Mathf.SmoothStep(0f, 1f, phaseT));
                }
                else
                {
                    var phaseT = Mathf.Clamp01((t - 0.68f) / 0.32f);
                    scale = Vector3.Lerp(stretchScale, defaultScale, Mathf.SmoothStep(0f, 1f, phaseT));
                }
                jumpOffset = 0.28f * intensity * Mathf.Sin(t * Mathf.PI);
            }
            else if (isBounceMode)
            {
                var bounceT = Mathf.Clamp01(mushroom.IdleAnimationTime / MushroomIdleBounceDuration);
                var squeezed = Vector3.Scale(defaultScale, MushroomIdleBounceScale);
                if (bounceT < 0.6f)
                {
                    scale = Vector3.Lerp(defaultScale, squeezed, Mathf.SmoothStep(0f, 1f, bounceT / 0.6f));
                }
                else
                {
                    scale = Vector3.Lerp(squeezed, defaultScale, Mathf.SmoothStep(0f, 1f, (bounceT - 0.6f) / 0.4f));
                }
            }

            mushroom.Renderer.transform.position = mushroom.BaseVisualPosition + Vector3.up * jumpOffset;
            mushroom.Renderer.transform.localScale = scale;
            mushroom.Renderer.transform.rotation = Quaternion.identity;
            if (isSpinMode && mushroom.IdleFlipMode == 1)
            {
                mushroom.IdleFlipTimer += deltaTime;
                if (!mushroom.IdleFlipBackDone && mushroom.IdleFlipTimer >= MushroomIdleFlipToggleInterval)
                {
                    mushroom.Renderer.flipX = mushroom.BaseFlipX;
                    mushroom.IdleFlipBackDone = true;
                }
            }

            if (t >= 1f)
            {
                StopMushroomIdleAnimation(mushroom);
                mushroom.NextIdleDelay = GetRandomMushroomIdleDelay(mushroom.Level);
            }
        }

        private void StopMushroomIdleAnimation(MushroomData mushroom)
        {
            if (mushroom == null || mushroom.Renderer == null) return;
            mushroom.IsIdleAnimationActive = false;
            mushroom.IdleAnimationTime = 0f;
            mushroom.Renderer.transform.position = mushroom.BaseVisualPosition;
            mushroom.Renderer.transform.localScale = mushroom.DefaultScale;
            mushroom.Renderer.transform.rotation = Quaternion.identity;
            mushroom.Renderer.flipX = mushroom.BaseFlipX;
            mushroom.IdleAnimationMode = MushroomIdleModeNone;
            mushroom.IdleFlipMode = 0;
            mushroom.IdleFlipTimer = 0f;
            mushroom.IdleFlipBackDone = false;
        }

        private int PickRandomIdleMode(int level)
        {
            if (level <= 2)
            {
                var roll = Random.Range(0, 4);
                if (roll == 0) return MushroomIdleModeJump;
                if (roll == 1) return MushroomIdleModeRotate;
                if (roll == 2) return MushroomIdleModeSpin;
                return MushroomIdleModeBounce;
            }

            var highRoll = Random.Range(0, 3);
            if (highRoll == 0) return MushroomIdleModeRotate;
            if (highRoll == 1) return MushroomIdleModeSpin;
            return MushroomIdleModeBounce;
        }

        private float GetRandomMushroomIdleDelay(int level)
        {
            var idx = Mathf.Clamp(level - 1, 0, _mushroomIdleMinDelay.Length - 1);
            return Random.Range(_mushroomIdleMinDelay[idx], _mushroomIdleMaxDelay[idx]);
        }

        private float GetMushroomIdleIntensity(int level)
        {
            var idx = Mathf.Clamp(level - 1, 0, _mushroomIdleIntensity.Length - 1);
            return _mushroomIdleIntensity[idx];
        }

        private void StartMushroomCurrencyAnimation(MushroomData mushroom, int amount)
        {
            if (mushroom == null || mushroom.Renderer == null) return;

            if (mushroom.IsIdleAnimationActive)
            {
                StopMushroomIdleAnimation(mushroom);
            }
            mushroom.IsCurrencyAnimationActive = true;
            mushroom.CurrencyAnimationTime = 0f;
            mushroom.PendingCurrencyAmount = amount;
            mushroom.CurrencyPopupTriggered = false;
            mushroom.DefaultScale = Vector3.one * MushroomScale;
            mushroom.Renderer.transform.position = mushroom.BaseVisualPosition;
            mushroom.Renderer.transform.rotation = Quaternion.identity;
            mushroom.Renderer.transform.localScale = mushroom.DefaultScale;
            mushroom.Renderer.flipX = mushroom.BaseFlipX;
        }

        private void UpdateMushroomCurrencyAnimation(MushroomData mushroom, float deltaTime)
        {
            if (mushroom == null || mushroom.Renderer == null) return;

            mushroom.CurrencyAnimationTime += deltaTime;
            var baseScale = mushroom.DefaultScale;
            var compressedScale = Vector3.Scale(baseScale, MushroomCurrencyCompressScale);
            var bounceTopScale = Vector3.Scale(baseScale, MushroomCurrencyBounceTopScale);

            if (mushroom.CurrencyAnimationTime <= MushroomCurrencyShakeDuration)
            {
                var t = Mathf.Clamp01(mushroom.CurrencyAnimationTime / MushroomCurrencyShakeDuration);
                var shakePhase = mushroom.CurrencyAnimationTime * MushroomCurrencyShakeFrequency * Mathf.PI * 2f;
                var shakeX = Mathf.Sin(shakePhase) * MushroomCurrencyShakeStrength;
                var scale = Vector3.Lerp(baseScale, compressedScale, t);
                scale.x += shakeX;
                scale.y -= Mathf.Abs(shakeX) * 0.35f;
                mushroom.Renderer.transform.localScale = scale;
                return;
            }

            var bounceTime = mushroom.CurrencyAnimationTime - MushroomCurrencyShakeDuration;
            if (bounceTime <= MushroomCurrencyBounceUpDuration)
            {
                var t = Mathf.Clamp01(bounceTime / MushroomCurrencyBounceUpDuration);
                mushroom.Renderer.transform.localScale = Vector3.Lerp(compressedScale, bounceTopScale, Mathf.SmoothStep(0f, 1f, t));
                return;
            }

            if (!mushroom.CurrencyPopupTriggered)
            {
                AddCurrency(mushroom.PendingCurrencyAmount);
                SpawnCurrencyPopup(mushroom, mushroom.PendingCurrencyAmount);
                mushroom.CurrencyPopupTriggered = true;
            }

            var returnTime = bounceTime - MushroomCurrencyBounceUpDuration;
            if (returnTime <= MushroomCurrencyBounceDownDuration)
            {
                var t = Mathf.Clamp01(returnTime / MushroomCurrencyBounceDownDuration);
                mushroom.Renderer.transform.localScale = Vector3.Lerp(bounceTopScale, baseScale, Mathf.SmoothStep(0f, 1f, t));
                return;
            }

            mushroom.Renderer.transform.localScale = baseScale;
            mushroom.IsCurrencyAnimationActive = false;
            mushroom.CurrencyAnimationTime = 0f;
            mushroom.PendingCurrencyAmount = 0;
            mushroom.CurrencyPopupTriggered = false;
            if (mushroom.QueueIdleAfterCurrency)
            {
                mushroom.NextIdleDelay = MushroomIdleDelayAfterCurrency;
                mushroom.QueueIdleAfterCurrency = false;
            }
            else
            {
                mushroom.NextIdleDelay = GetRandomMushroomIdleDelay(mushroom.Level);
            }
        }

        private void SpawnCurrencyPopup(MushroomData mushroom, int amount)
        {
            if (mushroom == null || mushroom.Renderer == null || _canvas == null || _mainCamera == null) return;

            var popupRoot = new GameObject("CurrencyPopup");
            popupRoot.transform.SetParent(_canvas.transform, false);
            var popupRect = popupRoot.AddComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(220f, 72f);

            var amountText = CreateText("Amount", new Vector2(0.5f, 0.5f), Vector2.zero, TextAnchor.MiddleRight, 30, popupRoot.transform);
            amountText.text = $"+{amount}";
            amountText.color = new Color(1f, 0.98f, 0.78f, 1f);
            amountText.fontStyle = FontStyle.Bold;
            var amountOutline = amountText.gameObject.AddComponent<Outline>();
            amountOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            amountOutline.effectDistance = new Vector2(1.5f, -1.5f);
            amountText.resizeTextForBestFit = true;
            amountText.resizeTextMinSize = 14;
            amountText.resizeTextMaxSize = 40;
            amountText.horizontalOverflow = HorizontalWrapMode.Wrap;
            amountText.verticalOverflow = VerticalWrapMode.Truncate;

            var textRect = amountText.rectTransform;
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(1f, 0.5f);
            textRect.anchoredPosition = new Vector2(-CurrencyPopupIconTextSpacing * 0.5f, 0f);
            textRect.sizeDelta = new Vector2(130f, 64f);

            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(popupRoot.transform, false);
            var iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(CurrencyPopupIconTextSpacing * 0.5f, 0f);
            iconRect.sizeDelta = new Vector2(38f, 38f);

            var iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = _coinSprite ?? _fallbackSprite;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;

            _currencyPopups.Add(new CurrencyPopupData
            {
                RootRect = popupRect,
                AmountText = amountText,
                Icon = iconImage,
                StartWorldPosition = (Vector2)mushroom.Renderer.transform.position + Vector2.up * CurrencyPopupStartOffsetY,
                RiseDuration = 0.28f,
                FadeDuration = 0.45f
            });
        }

        private void TickCurrencyPopups(float deltaTime)
        {
            if (_currencyPopups.Count == 0 || _canvas == null || _mainCamera == null) return;

            var canvasRect = _canvas.GetComponent<RectTransform>();
            for (var i = _currencyPopups.Count - 1; i >= 0; i--)
            {
                var popup = _currencyPopups[i];
                popup.Elapsed += deltaTime;

                var riseT = popup.RiseDuration <= 0f ? 1f : Mathf.Clamp01(popup.Elapsed / popup.RiseDuration);
                var yOffset = Mathf.SmoothStep(0f, 1f, riseT) * CurrencyPopupRiseHeight;
                var worldPos = popup.StartWorldPosition + Vector2.up * yOffset;
                var screenPos = _mainCamera.WorldToScreenPoint(worldPos);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out var localPoint);
                popup.RootRect.anchoredPosition = localPoint;

                var alpha = 1f;
                if (popup.Elapsed > popup.RiseDuration)
                {
                    var fadeT = popup.FadeDuration <= 0f ? 1f : Mathf.Clamp01((popup.Elapsed - popup.RiseDuration) / popup.FadeDuration);
                    alpha = 1f - fadeT;
                }

                SetCurrencyPopupAlpha(popup, alpha);

                if (popup.Elapsed >= popup.RiseDuration + popup.FadeDuration)
                {
                    if (popup.RootRect != null) Destroy(popup.RootRect.gameObject);
                    _currencyPopups.RemoveAt(i);
                }
            }
        }

        private static void SetCurrencyPopupAlpha(CurrencyPopupData popup, float alpha)
        {
            if (popup.AmountText != null)
            {
                var color = popup.AmountText.color;
                popup.AmountText.color = new Color(color.r, color.g, color.b, alpha);
            }

            if (popup.Icon != null)
            {
                var color = popup.Icon.color;
                popup.Icon.color = new Color(color.r, color.g, color.b, alpha);
            }
        }

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
            if (_isWarningVisible) return;
            _warningText.text = text;
            _warningFadeCoroutine = StartCoroutine(FadeWarning());
        }

        private IEnumerator FadeWarning()
        {
            _isWarningVisible = true;
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
            _isWarningVisible = false;
            _warningFadeCoroutine = null;
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
            _currencyText.text = _currency.ToString();
            _waveText.text = $"Waves Left: {Mathf.Max(0, MaxWaves - _currentWave)}";
            _nextWaveText.text = _waveInProgress ? "Next Wave: In Progress" : $"Next Wave: {Mathf.CeilToInt(Mathf.Max(0f, _timeToNextWave))}s";

            if (_spawnCostText != null) _spawnCostText.text = SpawnCost.ToString();
            if (_upgradeCostText != null)
            {
                var upgradePrice = (_selectedMushroom != null && _selectedMushroom.Level < 4)
                    ? _upgradeCostByLevel[_selectedMushroom.Level]
                    : _upgradeCostByLevel[1];
                _upgradeCostText.text = upgradePrice.ToString();
            }
            if (_healCostText != null) _healCostText.text = HealCost.ToString();

            _spawnButton.gameObject.SetActive(_selectionType == SelectionType.EmptyCell);
            _upgradeButton.gameObject.SetActive(_selectionType == SelectionType.Mushroom && !_waveInProgress && _selectedMushroom != null && _selectedMushroom.Level < 4);
            _healButton.gameObject.SetActive(_selectionType == SelectionType.Mushroom && !_waveInProgress && _selectedMushroom != null && _selectedMushroom.Health < GetMushroomMaxHp(_selectedMushroom.Level));
            UpdateActionButtonsPosition();
        }

        private void ConfigureActionButtonRect(Button button)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
        }

        private void UpdateActionButtonsPosition()
        {
            var canvasRect = _canvas.GetComponent<RectTransform>();
            var anchorWorld = GetActionAnchorWorldPosition();
            var rightWorld = anchorWorld
                + Vector2.right * (_cellSize * ActionButtonWorldOffsetXInCell)
                + Vector2.up * (_cellSize * ActionButtonWorldOffsetYInCell);

            var rightScreen = _mainCamera.WorldToScreenPoint(rightWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, rightScreen, null, out var localPoint);

            var spawnRect = _spawnButton.GetComponent<RectTransform>();
            var upgradeRect = _upgradeButton.GetComponent<RectTransform>();
            var healRect = _healButton.GetComponent<RectTransform>();

            spawnRect.anchoredPosition = localPoint;

            var visibleCount = (_upgradeButton.gameObject.activeSelf ? 1 : 0) + (_healButton.gameObject.activeSelf ? 1 : 0);
            if (visibleCount == 2)
            {
                var dy = ButtonHeight + ActionButtonVerticalGap;
                upgradeRect.anchoredPosition = localPoint;
                healRect.anchoredPosition = localPoint + Vector2.down * dy;
            }
            else
            {
                if (_upgradeButton.gameObject.activeSelf) upgradeRect.anchoredPosition = localPoint;
                if (_healButton.gameObject.activeSelf) healRect.anchoredPosition = localPoint;
            }
        }

        private Vector2 GetActionAnchorWorldPosition()
        {
            if (_selectionType == SelectionType.EmptyCell && _selectedCell != null)
            {
                return GetCellCenter(_selectedCell);
            }

            if (_selectionType == SelectionType.Mushroom && _selectedMushroom != null)
            {
                return GetCellCenter(_selectedMushroom.Cell);
            }

            return Vector2.zero;
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

        private Button CreateActionButton(
            string label,
            Vector2 anchor,
            Vector2 offset,
            UnityEngine.Events.UnityAction onClick,
            Sprite leftIcon,
            out Text costText,
            Transform parent = null,
            Sprite backgroundSprite = null)
        {
            var button = CreateButton(label, anchor, offset, onClick, parent, backgroundSprite);
            var root = button.GetComponent<RectTransform>();

            foreach (Transform child in root)
            {
                if (child.name == "Label")
                {
                    var labelText = child.GetComponent<Text>();
                    labelText.text = string.Empty;
                    break;
                }
            }

            if (leftIcon != null)
            {
                var leftWidth = ButtonWidth * LeftSideWidthRatio;
                var sidePadding = Mathf.Max(4f, ButtonWidth * 0.04f);
                var verticalPadding = Mathf.Max(4f, ButtonHeight * 0.1f);
                var leftIconSize = Mathf.Min(leftWidth - sidePadding * 2f, ButtonHeight - verticalPadding * 2f);
                leftIconSize *= SpawnIconScaleFactor;
                var leftCenterX = LeftSideWidthRatio * 0.5f;
                CreateButtonImage("LeftIcon", root, leftIcon, new Vector2(leftCenterX, LeftIconVerticalNormalized), new Vector2(leftIconSize, leftIconSize), false);
            }

            var rightStartX = LeftSideWidthRatio;
            var rightWidthRatio = 1f - LeftSideWidthRatio;
            var rightHalfCenterX = rightStartX + rightWidthRatio * 0.5f;
            const float costCoinGap = -14f;
            var rightHalfWidth = ButtonWidth * rightWidthRatio;
            var rightSidePadding = Mathf.Max(4f, ButtonWidth * 0.04f);
            var rightVerticalPadding = Mathf.Max(4f, ButtonHeight * 0.1f);
            var rightUsableWidth = Mathf.Max(20f, rightHalfWidth - rightSidePadding * 2f);
            var maxCoinByHeight = ButtonHeight - rightVerticalPadding * 2f;
            var coinSize = Mathf.Min(maxCoinByHeight, rightUsableWidth * 0.38f);
            coinSize *= CoinIconScaleFactor;
            var costWidth = Mathf.Max(20f, rightUsableWidth - coinSize - costCoinGap);
            var costFontSize = Mathf.Max(18, Mathf.RoundToInt(ButtonHeight * 0.28f));

            costText = CreateText("Cost", new Vector2(rightHalfCenterX, LeftIconVerticalNormalized), Vector2.zero, TextAnchor.MiddleRight, costFontSize, root);
            costText.rectTransform.pivot = new Vector2(1f, 0.5f);
            costText.rectTransform.anchoredPosition = new Vector2(-(coinSize * 0.5f + costCoinGap * 0.5f), 0f);
            costText.rectTransform.sizeDelta = new Vector2(costWidth, ButtonHeight);
            costText.text = "0";

            if (_coinSprite != null)
            {
                var coin = CreateButtonImage("CoinIcon", root, _coinSprite, new Vector2(rightHalfCenterX, LeftIconVerticalNormalized), new Vector2(coinSize, coinSize), true);
                var coinRect = coin.GetComponent<RectTransform>();
                coinRect.pivot = new Vector2(0f, 0.5f);
                coinRect.anchoredPosition = new Vector2(costCoinGap * 0.5f, 0f);
            }

            return button;
        }

        private static Image CreateButtonImage(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 size, bool compensateVerticalPivot)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = compensateVerticalPivot ? GetVerticalPivotCompensation(sprite, size) : Vector2.zero;
            rect.sizeDelta = size;

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            return image;
        }

        private static Vector2 GetVerticalPivotCompensation(Sprite sprite, Vector2 rectSize)
        {
            if (sprite == null || sprite.rect.height <= Mathf.Epsilon)
            {
                return Vector2.zero;
            }

            var normalizedPivotY = sprite.pivot.y / sprite.rect.height;
            var yOffset = (0.5f - normalizedPivotY) * rectSize.y;
            return new Vector2(0f, yOffset);
        }

        private Button CreateButton(string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction onClick, Transform parent = null, Sprite backgroundSprite = null)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent == null ? _canvas.transform : parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var sprite = backgroundSprite != null ? backgroundSprite : _buttonSprite;
            if (sprite != null)
            {
                image.sprite = sprite;
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

        private SpriteRenderer CreateMushroomBar(Transform parent, Color fillColor, float localY, float height, int sortingOrder)
        {
            var barRoot = new GameObject("MushroomBar");
            barRoot.transform.SetParent(parent, false);
            barRoot.transform.localPosition = new Vector3(0f, localY, 0f);

            var outline = new GameObject("Outline");
            outline.transform.SetParent(barRoot.transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localScale = new Vector3(MushroomBarWidth + MushroomBarOutlineWidthPadding, height + MushroomBarOutlineHeightPadding, 1f);
            var outlineRenderer = outline.AddComponent<SpriteRenderer>();
            outlineRenderer.sprite = _fallbackSprite;
            outlineRenderer.color = Color.black;
            outlineRenderer.sortingOrder = sortingOrder - 2;

            var background = new GameObject("Background");
            background.transform.SetParent(barRoot.transform, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(MushroomBarWidth, height, 1f);
            var backgroundRenderer = background.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = _fallbackSprite;
            backgroundRenderer.color = new Color(0.52f, 0.6f, 0.67f, 0.88f);
            backgroundRenderer.sortingOrder = sortingOrder - 1;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(barRoot.transform, false);
            fill.transform.localPosition = Vector3.zero;
            fill.transform.localScale = new Vector3(MushroomBarWidth, height, 1f);
            var fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = _fallbackSprite;
            fillRenderer.color = fillColor;
            fillRenderer.sortingOrder = sortingOrder;
            return fillRenderer;
        }

        private SpriteRenderer CreateMushroomBarsDivider(Transform parent, int sortingOrder)
        {
            var divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);
            divider.transform.localPosition = Vector3.zero;
            divider.transform.localScale = new Vector3(MushroomBarWidth + MushroomBarOutlineWidthPadding, MushroomBarsDividerHeight, 1f);

            var dividerRenderer = divider.AddComponent<SpriteRenderer>();
            dividerRenderer.sprite = _fallbackSprite;
            dividerRenderer.color = Color.black;
            dividerRenderer.sortingOrder = sortingOrder + 1;
            return dividerRenderer;
        }

        private void UpdateMushroomBars(MushroomData mushroom)
        {
            mushroom.WorldPosition = GetCellCenter(mushroom.Cell);
            mushroom.BarsRoot.transform.position = mushroom.Renderer.transform.position + Vector3.up * GetMushroomBarsPivotYOffset(mushroom.Level);
            var hpRatio = Mathf.Clamp01(mushroom.Health / GetMushroomMaxHp(mushroom.Level));
            var currencyRatio = Mathf.Clamp01(mushroom.CurrencyTimer / GetMushroomCurrencyInterval(mushroom.Level));
            var minFillWidth = MushroomBarWidth * 0.02f;
            var healthFillWidth = Mathf.Max(minFillWidth, hpRatio * MushroomBarWidth);
            var currencyFillWidth = Mathf.Max(minFillWidth, currencyRatio * MushroomBarWidth);
            mushroom.HealthBar.transform.localScale = new Vector3(healthFillWidth, mushroom.HealthBar.transform.localScale.y, 1f);
            mushroom.CurrencyBar.transform.localScale = new Vector3(currencyFillWidth, mushroom.CurrencyBar.transform.localScale.y, 1f);
            mushroom.HealthBar.transform.localPosition = new Vector3(-(MushroomBarWidth - healthFillWidth) * 0.5f, 0f, 0f);
            mushroom.CurrencyBar.transform.localPosition = new Vector3(-(MushroomBarWidth - currencyFillWidth) * 0.5f, 0f, 0f);

            var showBars = _selectedMushroom == mushroom;
            mushroom.CurrencyBar.transform.parent.gameObject.SetActive(showBars);
            mushroom.HealthBar.transform.parent.gameObject.SetActive(showBars);
            if (mushroom.CurrencyDivider != null)
            {
                mushroom.CurrencyDivider.gameObject.SetActive(showBars);
            }
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
        private float GetMushroomBarsPivotYOffset(int level) => _mushroomBarsPivotYOffset[level - 1];

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
            public Vector3 DefaultScale;
            public Vector3 BaseVisualPosition;
            public bool BaseFlipX;
            public bool IsCurrencyAnimationActive;
            public float CurrencyAnimationTime;
            public int PendingCurrencyAmount;
            public bool CurrencyPopupTriggered;
            public bool IsIdleAnimationActive;
            public float IdleAnimationTime;
            public float NextIdleDelay;
            public bool QueueIdleAfterCurrency;
            public int IdleAnimationMode;
            public int IdleFlipMode;
            public float IdleFlipTimer;
            public bool IdleFlipBackDone;
            public GameObject BarsRoot;
            public SpriteRenderer CurrencyBar;
            public SpriteRenderer HealthBar;
            public SpriteRenderer CurrencyDivider;
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

        private sealed class CurrencyPopupData
        {
            public RectTransform RootRect;
            public Text AmountText;
            public Image Icon;
            public Vector2 StartWorldPosition;
            public float Elapsed;
            public float RiseDuration;
            public float FadeDuration;
        }
    }
}
