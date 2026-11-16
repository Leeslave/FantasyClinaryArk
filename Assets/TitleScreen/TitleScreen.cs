using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FantasyClinaryArk
{
    public sealed class TitleScreen : MonoBehaviour
    {
        private const string LastActionKey = "Title.LastAction";
        private const string VolumeKey = "Settings.MasterVolume";
        private const float VideoLoadTimeout = 15f;

        [System.Serializable]
        private sealed class ActionLoop
        {
            public Texture2D atlas;
            public Vector2Int grid;
            public int frameCount;
            public float framesPerSecond;

            public Rect GetFrame(int frame)
            {
                float width = 1f / grid.x;
                float height = 1f / grid.y;
                // Sample inside each tile so bilinear filtering cannot pull in a neighboring frame.
                float insetX = 0.5f / atlas.width;
                float insetY = 0.5f / atlas.height;
                return new Rect(frame % grid.x * width + insetX,
                    1f - (frame / grid.x + 1) * height + insetY,
                    width - 2f * insetX, height - 2f * insetY);
            }
        }

        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RawImage cook;
        [SerializeField] private ActionLoop[] actions;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject introPanel;
        [SerializeField] private PotatoPancakeStage potatoPancake;
        [SerializeField] private Button newGame;
        [SerializeField] private Button continueGame;
        [SerializeField] private Button settings;
        [SerializeField] private Button closeSettings;
        [SerializeField] private Button skipIntro;
        [SerializeField] private GameObject quit;
        [SerializeField] private Slider volume;
        [SerializeField] private RawImage introImage;
        [SerializeField] private Text introStatus;
        [SerializeField] private VideoPlayer video;

        private static int sessionAction = -1;
        private Rect appliedSafeArea;
        private Vector2Int appliedScreenSize;
        private ActionLoop currentAction;
        private int frameIndex;
        private float loopTime;
        private float videoDeadline;
        private bool waitingForVideo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            sessionAction = -1;
        }

        private void Awake()
        {
            if (sessionAction < 0)
            {
                int lastAction = PlayerPrefs.GetInt(LastActionKey, -1);
                if (lastAction >= 0 && lastAction < actions.Length && actions.Length > 1)
                {
                    sessionAction = Random.Range(0, actions.Length - 1);
                    if (sessionAction >= lastAction)
                        sessionAction++;
                }
                else
                {
                    sessionAction = Random.Range(0, actions.Length);
                }

                PlayerPrefs.SetInt(LastActionKey, sessionAction);
                PlayerPrefs.Save();
            }

            currentAction = actions[sessionAction];
            cook.texture = currentAction.atlas;
            cook.uvRect = currentAction.GetFrame(0);
            quit.SetActive(!Application.isMobilePlatform);
            RefreshContinue();

            float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
            volume.SetValueWithoutNotify(savedVolume);
            AudioListener.volume = savedVolume;
            volume.onValueChanged.AddListener(SetVolume);

            video.prepareCompleted += PlayIntro;
            video.frameReady += ShowFirstFrame;
            video.loopPointReached += FinishIntro;
            video.errorReceived += HandleIntroError;
        }

        private void Start()
        {
            ApplySafeArea();
            Focus(newGame);
        }

        private void Update()
        {
            ApplySafeArea();

            if (titlePanel.activeSelf)
                AdvanceCookAnimation();

            if (waitingForVideo && Time.unscaledTime >= videoDeadline)
                HandleIntroError(video, "The video did not start before the playback timeout.");
        }

        private void AdvanceCookAnimation()
        {
            float loopDuration = currentAction.frameCount / currentAction.framesPerSecond;
            loopTime = (loopTime + Time.unscaledDeltaTime) % loopDuration;
            int nextFrame = Mathf.Min((int)(loopTime * currentAction.framesPerSecond), currentAction.frameCount - 1);
            if (nextFrame == frameIndex)
                return;

            frameIndex = nextFrame;
            cook.uvRect = currentAction.GetFrame(frameIndex);
        }

        private void ApplySafeArea()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;
            if (area == appliedSafeArea && size == appliedScreenSize)
                return;

            appliedSafeArea = area;
            appliedScreenSize = size;
            safeArea.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            safeArea.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
        }

        public void StartNewGame()
        {
            titlePanel.SetActive(false);
            introPanel.SetActive(true);
            introImage.texture = null;
            introImage.enabled = false;
            introStatus.text = "인트로 불러오는 중…";
            introStatus.enabled = true;
            waitingForVideo = true;
            videoDeadline = Time.unscaledTime + VideoLoadTimeout;
            video.sendFrameReadyEvents = true;
            Focus(skipIntro);
            video.Prepare();
        }

        public void OpenSettings()
        {
            titlePanel.SetActive(false);
            settingsPanel.SetActive(true);
            Focus(closeSettings);
        }

        public void CloseSettings()
        {
            PlayerPrefs.Save();
            settingsPanel.SetActive(false);
            titlePanel.SetActive(true);
            Focus(settings);
        }

        public void SkipIntro()
        {
            FinishIntro(video);
        }

        public void ReturnToTitle()
        {
            potatoPancake.gameObject.SetActive(false);
            RefreshContinue();
            titlePanel.SetActive(true);
            Focus(newGame);
        }

        public void ContinueGame()
        {
            if (!potatoPancake.IsRecipeUnlocked)
                return;

            titlePanel.SetActive(false);
            potatoPancake.OpenRecipe(ReturnToTitle);
        }

        private void RefreshContinue()
        {
            bool unlocked = potatoPancake.IsRecipeUnlocked;
            continueGame.interactable = unlocked;
            continueGame.GetComponentInChildren<Text>(true).color = unlocked
                ? new Color32(255, 232, 192, 255)
                : new Color32(163, 138, 120, 255);

            Navigation newGameNavigation = newGame.navigation;
            newGameNavigation.selectOnDown = unlocked ? continueGame : settings;
            newGame.navigation = newGameNavigation;

            Navigation settingsNavigation = settings.navigation;
            settingsNavigation.selectOnUp = unlocked ? continueGame : newGame;
            settings.navigation = settingsNavigation;
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void SetVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
        }

        private void PlayIntro(VideoPlayer player)
        {
            if (waitingForVideo && introPanel.activeSelf)
                player.Play();
        }

        private void ShowFirstFrame(VideoPlayer player, long frame)
        {
            if (!waitingForVideo || !introPanel.activeSelf)
                return;

            waitingForVideo = false;
            player.sendFrameReadyEvents = false;
            introImage.texture = player.texture;
            introImage.enabled = true;
            introStatus.enabled = false;
        }

        private void FinishIntro(VideoPlayer player)
        {
            if (!introPanel.activeSelf)
                return;

            waitingForVideo = false;
            player.sendFrameReadyEvents = false;
            player.Stop();
            introImage.texture = null;
            introPanel.SetActive(false);
            potatoPancake.Begin(ReturnToTitle);
        }

        private void HandleIntroError(VideoPlayer player, string error)
        {
            if (!introPanel.activeSelf)
                return;

            Debug.LogWarning($"Intro playback failed: {error}", this);
            waitingForVideo = false;
            player.sendFrameReadyEvents = false;
            player.Stop();
            introImage.texture = null;
            introImage.enabled = false;
            introStatus.text = "인트로를 재생할 수 없습니다.";
            introStatus.enabled = true;
        }

        private static void Focus(Selectable item)
        {
            EventSystem.current.SetSelectedGameObject(item.gameObject);
        }

        private void OnDestroy()
        {
            video.prepareCompleted -= PlayIntro;
            video.frameReady -= ShowFirstFrame;
            video.loopPointReached -= FinishIntro;
            video.errorReceived -= HandleIntroError;
        }
    }
}
