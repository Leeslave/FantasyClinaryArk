using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static FantasyClinaryArk.Px;

namespace FantasyClinaryArk
{
    /// <summary>
    /// Gesture minigames drawn as 320 × 180 pixel art,
    /// with the step card, grading stamp and result screen built in uGUI on the 1280 × 720 composition.
    /// </summary>
    public sealed class PotatoKitchen : MonoBehaviour
    {
        internal const int ArtW = 320, ArtH = 180;
        private const float ScreenW = 1280, ScreenH = 720;

        private enum Demo { Down, UpDown, Hold, Right, Circle, Scrub, Up, Tap, Left }
        private enum Mode { Idle, Intro, Ready, Play, Graded, Result }

        private sealed class StepDef
        {
            public string Name, Desc, Hint, Clue;
            public int Time;
            public bool Stove;
            public Demo Demo;
            public Func<KitchenContext, CookingGame> Make;
        }

        private static readonly StepDef[] PotatoSteps =
        {
            new StepDef
            {
                Name = "감자 껍질 벗기기", Time = 20, Demo = Demo.Down, Make = k => new PeelGame(k),
                Desc = "중간 크기 감자 한 알(약 150g)이면 지름 14cm 한 판이 나와요. 필러로 위에서 아래로 쓸어 내려 껍질을 벗겨요.",
                Hint = "위에서 아래로 쓸어 내리기", Clue = "부엌에 남은 건 감자 몇 알뿐이었다."
            },
            new StepDef
            {
                Name = "강판에 갈기", Time = 12, Demo = Demo.UpDown, Make = k => new GrateGame(k),
                Desc = "손질한 감자(약 135g)를 강판에 대고 위아래 끝까지 빠르게 문질러요. 열두 번이면 다 갈려요.",
                Hint = "누른 채 위아래로 문지르기", Clue = "숙소에서 강판에 감자를 문지르던 소리가 났다."
            },
            new StepDef
            {
                Name = "면포로 물기 짜기", Time = 12, Demo = Demo.Hold, Make = k => new SqueezeGame(k),
                Desc = "면포를 꾹 누르고 있으면 감자 물이 빠져요. 약 40ml, 게이지가 초록 구간일 때 손을 떼요.",
                Hint = "꾹 누르다가 초록에서 떼기", Clue = "젖은 감자는 체 위에 놓여 있었고, 그 아래에는 빈 그릇이 있었다."
            },
            new StepDef
            {
                Name = "윗물 따라 내기", Time = 20, Demo = Demo.Right, Make = k => new PourGame(k),
                Desc = "5분 두면 바닥에 흰 전분(약 5g)이 가라앉아요. 그릇을 오른쪽으로 기울여 맑은 윗물만 버려요. 너무 기울이면 전분까지 쏟아져요!",
                Hint = "누른 채 오른쪽으로 끌어 기울이기", Clue = "맑은 윗물은 버렸지만 바닥에 붙은 흰 전분은 감자 반죽에 다시 넣었다."
            },
            new StepDef
            {
                Name = "소금 넣고 섞기", Time = 14, Demo = Demo.Circle, Make = k => new MixGame(k),
                Desc = "소금통을 톡톡 두 번 눌러 한 꼬집(약 0.5g)을 넣고, 숟가락으로 원을 그리며 세 바퀴 섞어요.",
                Hint = "소금통 톡톡 → 빙글빙글 젓기", Clue = "그날 하츠는 손끝으로 작은 소금 한 꼬집을 집었다."
            },
            new StepDef
            {
                Name = "기름 두르고 반죽 펴기", Time = 18, Stove = true, Demo = Demo.Scrub, Make = k => new SpreadGame(k),
                Desc = "기름병을 꾹 눌러 1큰술쯤(13~20ml) 붓다가 초록 구간에서 떼요. 그다음 반죽을 점선 안쪽에 지름 14cm쯤으로 얇게 펴요.",
                Hint = "꾹 눌러 붓기 → 문질러 펴기", Clue = "에이미는 감자전 가장자리가 얇고 바삭했다고 기억한다."
            },
            new StepDef
            {
                Name = "노릇하게 굽고 뒤집기", Time = 22, Stove = true, Demo = Demo.Up, Make = k => new FryGame(k),
                Desc = "가장자리가 노릇해지면 뒤집개로 위로 휙 쓸어 뒤집어요. 반대쪽도 노릇해지면 한 번 더 휙! 접시에 담아요.",
                Hint = "노릇할 때 위로 휙 쓸어 올리기", Clue = "노릇해진 가장자리가 단단해지자 뒤집개가 그 밑으로 들어갔다."
            }
        };

        private static readonly StepDef[] UdonSteps =
        {
            new StepDef
            {
                Name = "대파 큼직하게 썰기", Time = 15, Demo = Demo.Down, Make = k => new SliceScallionGame(k),
                Desc = "대파를 3~4cm 길이로 큼직하게 썰어요. 표시선을 위에서 아래로 그어 보세요.",
                Hint = "표시선을 따라 아래로 쓸기", Clue = "국물 속 대파는 한입에 다 넣기 어려울 만큼 큼직했다."
            },
            new StepDef
            {
                Name = "대파 강불에 굽기", Time = 16, Stove = true, Demo = Demo.Tap, Make = k => new RoastScallionGame(k),
                Desc = "기름 없이 강불에 구워요. 대파의 겉이 살짝 그을리고 게이지가 초록 구간에 들어오면 표시된 대파를 눌러 뒤집어요.",
                Hint = "살짝 그을린 대파 톡 눌러 뒤집기", Clue = "기름을 두르지 않은 팬에서도 대파 향이 확 올라왔다."
            },
            new StepDef
            {
                Name = "물과 쯔유 붓기", Time = 20, Stove = true, Demo = Demo.Hold, Make = k => new UdonBrothGame(k),
                Desc = "병을 꾹 눌러 물 600ml를 붓고 손을 떼요. 이어서 쯔유 병을 눌러 50~100ml 사이로 간을 맞춰요.",
                Hint = "병을 길게 누르고 초록 구간에서 떼기", Clue = "대파 향이 올라오면 물을 붓고 쯔유를 넣었다."
            },
            new StepDef
            {
                Name = "후추 넉넉히 넣기", Time = 12, Stove = true, Demo = Demo.Tap, Make = k => new UdonPepperGame(k),
                Desc = "후추 향이 강하게 올라올 때까지 후추통을 톡톡 눌러요. 이 국물의 맛을 결정하는 재료예요.",
                Hint = "후추통 톡톡 누르기", Clue = "첫 모금부터 후추의 매운맛이 또렷했다."
            },
            new StepDef
            {
                Name = "우동면 풀어 담기", Time = 18, Stove = true, Demo = Demo.Left, Make = k => new UdonNoodleGame(k),
                Desc = "오른쪽의 우동면을 냄비로 끌어 넣어요. 면이 풀리면 냄비를 눌러 그릇에 담아요.",
                Hint = "면을 냄비로 끌기 → 면이 익으면 냄비 누르기", Clue = "후추의 매운맛이 올라온 뒤에 우동면을 넣었다."
            }
        };

        private static readonly string[] Dishes = { "산골 마을의 황금 감자전", "노릇노릇 감자전", "소박한 감자전", "새카만 감자전" };

        private static readonly string[] HartzVerdict =
        {
            "좋아, 양쪽 다 노릇하게 익었어. 식기 전에 가져가자.",
            "이 정도면 그날 맛이 날 거야. 얼른 가져가자.",
            "음… 그래도 정성은 들어갔어. 가져가 보자.",
            "…일단 가져가 보자. 에이미라면 이해해 줄 거야."
        };

        private static readonly Color Gold = Hex("#EDB56F"), Cream = Hex("#FFEBC7"), Muted = Hex("#D9BC9F"), Ink = Hex("#392527"),
            Crimson = Hex("#7A2A30"), PanelFill = Hex("#25191A"), Edge = Hex("#C98A62"), Border = Hex("#8C6042"), Dark = Hex("#120b0c"),
            Stroke = Hex("#1a1012");

        private Font headingFont, bodyFont;
        private Texture hartz;
        private Action<int> served;
        private UnityAction exit;

        private KitchenContext k;
        private PixelCanvas art;
        private RectTransform frame, ui;
        private RawImage artImage, restaurantImage;
        private Image restaurantDim;
        private KitchenSurface surface;
        private AudioSource audioSource;
        private RenderTexture restaurantPixels;
        private Texture restaurantSource;

        private Mode mode = Mode.Idle;
        private int step, total, tier;
        private StepDef def;
        private CookingGame game;
        private float st, timeLeft, typed;
        private int lastSecond;
        private StepDef[] steps;
        private int[] scores;
        private bool isUdon;
        private readonly List<(float at, Sfx sfx)> queued = new List<(float, Sfx)>();
        private readonly List<UnityEngine.Object> sceneAssets = new List<UnityEngine.Object>();

        // Per-screen UI references.
        private RectTransform playPanel, timerFill, hand, holdFrame, gradedRoot, gradedDetails, readyRect, stampRect;
        private Image timerFillImage, gradedDim;
        private Image[] holdEdges;
        private RectTransform[] trail;
        private Text timerText, shout, readyText, stampText, typedText;
        private Button nextButton;
        private string typedLine = "";
        private readonly List<GaugeView> gaugeViews = new List<GaugeView>();
        private readonly List<Gauge> gauges = new List<Gauge>();

        private static Sprite stepped;
        private static Texture2D cookGradient, vignette, handTexture;

        // ================================================================== setup & flow
        internal void Setup(Font heading, Font body, Texture restaurant, Texture hartzPortrait)
        {
            headingFont = heading;
            bodyFont = body;
            hartz = hartzPortrait;
            KitchenArt.Build();
            BuildStaticTextures();

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            k = new KitchenContext { Sound = new KitchenSound(audioSource) };
            art = new PixelCanvas(ArtW, ArtH);

            var root = (RectTransform)transform;
            var backdrop = gameObject.AddComponent<Image>();
            backdrop.color = Dark;
            backdrop.raycastTarget = true;

            // Everything is laid out on a fixed 1280 × 720 frame centred in the stage.
            frame = (RectTransform)new GameObject("Kitchen frame", typeof(RectTransform)).transform;
            frame.SetParent(root, false);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.sizeDelta = new Vector2(ScreenW, ScreenH);
            frame.gameObject.AddComponent<RectMask2D>();

            restaurantImage = Raw(frame, "Restaurant", 0, 0, ScreenW, ScreenH, null);
            restaurantSource = restaurant;
            if (restaurant != null)
            {
                restaurantPixels = new RenderTexture(ArtW, ArtH, 0) { filterMode = FilterMode.Point, name = "Restaurant pixels" };
                restaurantImage.texture = restaurantPixels;
            }

            restaurantDim = Rect(frame, 0, 0, ScreenW, ScreenH, Rgba(20, 11, 10, .6f));

            artImage = Raw(frame, "Art", 0, 0, ScreenW, ScreenH, art.Texture);
            artImage.raycastTarget = true;
            surface = artImage.gameObject.AddComponent<KitchenSurface>();
            Selectable selectable = artImage.gameObject.AddComponent<Selectable>();
            selectable.transition = Selectable.Transition.None;
            selectable.navigation = new Navigation { mode = Navigation.Mode.None };
            surface.Down = PointerDown;
            surface.Move = PointerMove;
            surface.Up = PointerUp;
            surface.Hover = p =>
            {
                k.Pointer = p;
                k.Hovering = true;
            };
            surface.Leave = () =>
            {
                k.Hovering = false;
                if (!k.PointerDown)
                    k.Pointer = new Vector2(-99, -99);
            };
            surface.Submit = SubmitKey;

            Raw(frame, "Vignette", 0, 0, ScreenW, ScreenH, vignette);
            ui = Box(frame, "Screen", 0, 0, ScreenW, ScreenH);
        }

        internal void Begin(Action<int> onServed, UnityAction onExit, bool udon = false)
        {
            served = onServed;
            exit = onExit;
            isUdon = udon;
            steps = udon ? UdonSteps : PotatoSteps;
            scores = new int[steps.Length];
            queued.Clear();
            k.PointerDown = false;
            k.Hovering = false;
            Retry();
        }

        private void GoIntro(int index)
        {
            ClearScreen();
            mode = Mode.Intro;
            step = index;
            def = steps[index];
            game = def.Make(k);
            k.Parts.Clear();
            st = 0;
            ShowRestaurant(false);
            BuildIntro();
        }

        private void StartPlay()
        {
            ClearScreen();
            mode = Mode.Ready;
            st = 0;
            timeLeft = def.Time;
            lastSecond = 99;
            k.Parts.Clear();
            BuildPlay();
            Focus(surface.gameObject);
        }

        private void Finish(int score, string note)
        {
            score = Mathf.RoundToInt(Clamp(score, 0, 100));
            mode = Mode.Graded;
            st = 0;
            scores[step] = score;
            Grade grade = GradeOf(score);
            k.Play(Sfx.Stamp);
            queued.Add((k.Time + .18f, grade.Sound));
            if (score >= 90)
                for (int i = 0; i < 40; i++)
                {
                    float angle = Rnd() * Mathf.PI * 2, speed = 40 + Rnd() * 60;
                    k.Parts.Emit(160, 72, Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed - 20, Hex(Rnd() < .5f ? "#FFC05A" : "#fff1b3"), 1.1f, 70, Rnd() < .4f ? 2 : 1, float.NaN, false, true);
                }

            playPanel.gameObject.SetActive(false);
            BuildGraded(score, note, grade, game.Measure());
        }

        private void Next()
        {
            if (step + 1 < steps.Length)
                GoIntro(step + 1);
            else
                GoResult();
        }

        private void GoResult()
        {
            ClearScreen();
            mode = Mode.Result;
            st = 0;
            typed = 0;
            int sum = 0;
            foreach (int s in scores)
                sum += Mathf.Max(0, s);
            total = Mathf.RoundToInt((float)sum / scores.Length);
            tier = total >= 90 ? 0 : total >= 70 ? 1 : total >= 45 ? 2 : 3;
            queued.Add((k.Time + .2f, tier < 2 ? Sfx.Perfect : Sfx.Ok));
            k.Parts.Clear();
            ShowRestaurant(true);
            BuildResult();
        }

        private void Retry()
        {
            for (int i = 0; i < scores.Length; i++)
                scores[i] = -1;
            k.PancakeMask = null;
            k.FinalPancake = null;
            k.Amounts = new Batch();
            k.Udon = new UdonBatch();
            GoIntro(0);
        }

        private void ShowRestaurant(bool show)
        {
            if (show && restaurantPixels != null)
            {
                // Downsample the painted restaurant so it sits with the pixel art.
                RenderTexture half = RenderTexture.GetTemporary(ArtW * 2, ArtH * 2);
                Graphics.Blit(restaurantSource, half);
                Graphics.Blit(half, restaurantPixels);
                RenderTexture.ReleaseTemporary(half);
            }

            restaurantImage.enabled = show && restaurantImage.texture != null;
            restaurantDim.enabled = show;
        }

        // ================================================================== input
        private void PointerDown(Vector2 p)
        {
            k.Pointer = p;
            k.PointerDown = true;
            k.Hovering = true;
            if (mode == Mode.Play)
                game.Down(p);
            else if (mode == Mode.Graded && st > 1.1f)
                Next();
        }

        private void PointerMove(Vector2 p)
        {
            k.Pointer = p;
            if (mode == Mode.Play && k.PointerDown)
                game.Move(p);
        }

        private void PointerUp(Vector2 p)
        {
            k.Pointer = p;
            bool was = k.PointerDown;
            k.PointerDown = false;
            if (was && mode == Mode.Play)
                game.Up(p);
        }

        private void SubmitKey()
        {
            if (mode == Mode.Play)
                game.Submit();
            else if (mode == Mode.Graded && st > .5f)
                Next();
        }

        private void OnDisable()
        {
            k?.Parts.Clear();
            if (k != null)
            {
                k.PointerDown = false;
                k.Hovering = false;
            }
        }

        private void OnDestroy()
        {
            ClearScreen();
            if (restaurantPixels != null)
                restaurantPixels.Release();
        }

        // ================================================================== frame loop
        private void Update()
        {
            if (mode == Mode.Idle)
                return;
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            k.Time += dt;
            st += dt;
            for (int i = queued.Count - 1; i >= 0; i--)
                if (k.Time >= queued[i].at)
                {
                    k.Play(queued[i].sfx);
                    queued.RemoveAt(i);
                }

            switch (mode)
            {
                case Mode.Intro:
                    game.Update(dt, false);
                    AnimateDemo();
                    break;
                case Mode.Ready:
                    game.Update(dt, false);
                    if (st > 1.25f)
                    {
                        mode = Mode.Play;
                        st = 0;
                        readyRect.gameObject.SetActive(false);
                    }

                    break;
                case Mode.Play:
                    timeLeft -= dt;
                    game.Update(dt, true);
                    int second = Mathf.CeilToInt(timeLeft);
                    if (timeLeft <= 3 && second != lastSecond && second > 0)
                        k.Play(Sfx.Tick);
                    lastSecond = second;
                    if (game.Done)
                        Finish(game.Score, game.Note);
                    else if (timeLeft <= 0)
                    {
                        (int score, string note) = game.Timeout();
                        Finish(score, note);
                    }

                    break;
                case Mode.Graded:
                    game.Update(dt, false);
                    break;
                case Mode.Result:
                    typed += dt * 30;
                    if (Rnd() < .5f)
                        k.Parts.Emit(90 + Rnd() * 44, 70 + Rnd() * 16, (Rnd() - .5f) * 4, -12, Rgba(255, 255, 255, .45f), 2.2f, -3, Rnd() < .5f ? 2 : 1, float.NaN, false, true);
                    break;
            }

            k.Parts.Update(dt);
            DrawArt();
            UpdateScreen();
        }

        private void DrawArt()
        {
            if (mode == Mode.Result)
            {
                art.Clear(None);
                if (isUdon)
                    UdonArt.Bowl(art, 112, 92, k.Udon.Pepper);
                else
                {
                    art.Ellipse(112, 100, 90, 56, Rgba(0, 0, 0, .35f));
                    art.Blit(KitchenArt.Plate, 112 - 95, 88 - 60);
                    if (k.FinalPancake != null)
                        art.BlitScaled(k.FinalPancake, 112 - 63, 88 - 43, 126, 86);
                    else
                        art.Ellipse(112, 88, 56, 36, KitchenArt.CookColor(.7f));
                    art.Blit(KitchenArt.Dip, 186, 120);
                }
                k.Parts.Draw(art);
            }
            else
            {
                Array.Copy((def.Stove ? KitchenArt.StoveBg : KitchenArt.CounterBg).Px, art.Buf, art.Buf.Length);
                game.Art(art);
                if (mode != Mode.Intro)
                {
                    k.Parts.Draw(art);
                    game.ArtTop(art);
                }
            }

            art.Upload();

            Vector2 shake = Vector2.zero;
            if (k.Shake > 0)
            {
                shake = new Vector2(Mathf.Round((Rnd() - .5f) * k.Shake * 10), Mathf.Round((Rnd() - .5f) * k.Shake * 10));
                k.Shake = Mathf.Max(0, k.Shake - Time.unscaledDeltaTime * 2.5f);
            }

            artImage.rectTransform.anchoredPosition = new Vector2(shake.x, -shake.y);
        }

        private void UpdateScreen()
        {
            if (timerFill != null)
            {
                float f = Clamp(timeLeft / def.Time, 0, 1);
                bool low = timeLeft < 4 && mode == Mode.Play;
                timerFill.sizeDelta = new Vector2(174 * f, 12);
                timerFillImage.color = low ? Hex(Mathf.FloorToInt(k.Time * 6) % 2 == 1 ? "#e07a6a" : "#b5443a") : Hex("#E0A43A");
                timerText.text = Mathf.Max(0, Mathf.CeilToInt(timeLeft)) + "초";
                timerText.color = low ? Hex("#ff9d8a") : Cream;
            }

            if (mode == Mode.Ready || mode == Mode.Play)
            {
                gauges.Clear();
                game.Gauges(gauges);
                for (int i = 0; i < gaugeViews.Count; i++)
                    gaugeViews[i].Root.gameObject.SetActive(i < gauges.Count);
                for (int i = 0; i < gauges.Count; i++)
                {
                    if (i == gaugeViews.Count)
                        gaugeViews.Add(new GaugeView(this, playPanel, 960, 136 + i * 114));
                    gaugeViews[i].Set(gauges[i]);
                }

                Callout? callout = mode == Mode.Play ? game.Shout : null;
                shout.gameObject.SetActive(callout.HasValue);
                if (callout.HasValue)
                {
                    shout.text = callout.Value.Text;
                    shout.color = callout.Value.Color;
                    shout.fontSize = callout.Value.Size;
                }
            }

            if (mode == Mode.Ready)
            {
                bool go = st >= .85f;
                float t = go ? (st - .85f) / .4f : st / .85f;
                float scale = Lerp(1.6f, 1, EaseOutBack(Clamp(t * 1.6f, 0, 1)));
                readyRect.localScale = Vector3.one * scale;
                readyText.text = go ? "시작!" : "준비…";
                readyText.color = go ? Hex("#FFD37A") : Cream;
            }

            if (mode == Mode.Graded)
            {
                gradedDim.color = Rgba(18, 10, 12, Clamp(st * 2, 0, .45f));
                float t = Clamp(st / .28f, 0, 1);
                stampRect.localScale = Vector3.one * Lerp(2.4f, 1, EaseOutBack(t));
                Color c = stampText.color;
                c.a = t;
                stampText.color = c;
                if (st > .3f && !gradedDetails.gameObject.activeSelf)
                {
                    gradedDetails.gameObject.SetActive(true);
                    Focus(nextButton.gameObject);
                }
            }

            if (typedText != null)
                typedText.text = typedLine.Substring(0, Mathf.Min(typedLine.Length, Mathf.FloorToInt(typed)));
        }

        private void AnimateDemo()
        {
            const float bx = 330, by = 404, bw = 300, bh = 150;
            float time = k.Time, t = time % 1.6f / 1.6f, cx = bx + bw / 2, cy = by + bh / 2, hx = cx, hy = cy;
            bool pressed = true;
            holdFrame.gameObject.SetActive(def.Demo == Demo.Hold);
            switch (def.Demo)
            {
                case Demo.Down:
                    hx = cx - 60 + Mathf.FloorToInt(time / 1.6f) % 4 * 40;
                    hy = by + 30 + t * (bh - 70);
                    break;
                case Demo.UpDown:
                    hy = cy + Mathf.Sin(time * 7) * 40;
                    break;
                case Demo.Hold:
                    pressed = t < .75f;
                    float r = 10 + t * 44;
                    Place(holdFrame, cx - r, cy - r, r * 2, r * 2);
                    Color c = Hex(t < .5f ? "#7fb5d6" : t < .75f ? "#8fd0a0" : "#d86a5a");
                    foreach (Image edge in holdEdges)
                        edge.color = c;
                    break;
                case Demo.Right:
                case Demo.Left:
                    float travel = Clamp(t * 1.4f, 0, 1);
                    hx = bx + 50 + (def.Demo == Demo.Left ? 1 - travel : travel) * (bw - 110);
                    pressed = t < .8f;
                    break;
                case Demo.Tap:
                    pressed = t * 3 % 1 < .25f;
                    hy -= pressed ? 0 : 10;
                    break;
                case Demo.Circle:
                    hx = cx + Mathf.Cos(time * 4.5f) * 70;
                    hy = cy + Mathf.Sin(time * 4.5f) * 40;
                    break;
                case Demo.Scrub:
                    hx = cx + Mathf.Sin(time * 5) * 90;
                    hy = cy + Mathf.Sin(time * 11) * 30;
                    break;
                case Demo.Up:
                    hy = by + bh - 26 - Mathf.Pow(Clamp(t * 1.6f, 0, 1), 2) * (bh - 60);
                    pressed = t < .62f;
                    break;
            }

            for (int i = 0; i < trail.Length; i++)
            {
                float tt = time - (i + 1) * .05f, px = hx, py = hy;
                bool show = true;
                if (def.Demo == Demo.Circle)
                {
                    px = cx + Mathf.Cos(tt * 4.5f) * 70;
                    py = cy + Mathf.Sin(tt * 4.5f) * 40;
                }
                else if (def.Demo == Demo.UpDown)
                    py = cy + Mathf.Sin(tt * 7) * 40;
                else if (def.Demo == Demo.Scrub)
                {
                    px = cx + Mathf.Sin(tt * 5) * 90;
                    py = cy + Mathf.Sin(tt * 11) * 30;
                }
                else
                    show = false;

                trail[i].gameObject.SetActive(show);
                Place(trail[i], px - 3, py - 3, 6, 6);
            }

            Place(hand, hx - 3.5f * 4, hy - 4 + (pressed ? 2 : 0), 48, 52);
        }

        // ================================================================== screens
        private void BuildHud(bool play)
        {
            Panel(ui, 20, 14, 360, 74, Edge, PanelFill);
            Panel(ui, 36, 30, 58, 42, Edge, Crimson, false);
            TextMid(ui, isUdon ? "1-2" : "1-1", 65, 52, 19, true, Gold, TextAnchor.MiddleCenter);
            Txt(ui, isUdon ? "겐로쿠 우동" : "산골 마을의 황금 감자전", 110, 48, 19, true, Cream);
            Txt(ui, $"STEP {step + 1} · {def.Name}", 110, 72, 14, false, Gold);

            for (int i = 0; i < steps.Length; i++)
            {
                float x = 432 + i * 54, y = 26;
                bool current = i == step, done = scores[i] >= 0 && !current;
                Grade grade = done ? GradeOf(scores[i]) : default;
                Panel(ui, x, y, 44, 46, current ? Hex("#FFC05A") : done ? grade.Chip : Hex("#52342C"),
                    done ? Hex("#2e1d1e") : current ? Hex("#4a2a22") : Hex("#1f1415"), false);
                TextMid(ui, (i + 1).ToString(), x + 22, y + 24, 18, false, current ? Hex("#FFC05A") : done ? grade.Color : Hex("#9c8676"),
                    TextAnchor.MiddleCenter, true);
            }

            if (play)
            {
                const float x = 842, y = 24, w = 296;
                Panel(ui, x, y, w, 52, Border, PanelFill, false);
                foreach (var r in new[] { new Vector4(18, 14, 18, 3), new Vector4(18, 35, 18, 3), new Vector4(21, 17, 12, 4), new Vector4(24, 21, 6, 4), new Vector4(21, 27, 12, 8) })
                    Rect(ui, x + r.x, y + r.y, r.z, r.w, Cream);
                Rect(ui, x + 23, y + 29, 8, 5, Gold);
                Rect(ui, x + 48, y + 18, 178, 16, Dark);
                timerFillImage = Rect(ui, x + 50, y + 20, 174, 12, Hex("#E0A43A"));
                timerFill = timerFillImage.rectTransform;
                Stretch(Rect(timerFill, 0, 0, 0, 3, Rgba(255, 255, 255, .25f)), new Vector2(0, 1), new Vector2(1, 1));
                timerText = TextMid(ui, "", x + w - 18, y + 27, 18, false, Cream, TextAnchor.MiddleRight, true);
            }

            Btn(ui, 1150, 24, 102, 52, "나가기", () => exit?.Invoke(), false, 18, false, false);
        }

        private void BuildIntro()
        {
            BuildHud(false);
            Rect(ui, 0, 100, ScreenW, ScreenH - 100, Rgba(18, 10, 12, .55f));
            const float x = 290, y = 122, w = 700, h = 540;
            Parchment(ui, x, y, w, h);
            Panel(ui, x + 40, y + 36, 150, 40, Edge, Crimson, false);
            TextMid(ui, $"STEP {step + 1} / {steps.Length}", x + 115, y + 57, 17, false, Cream, TextAnchor.MiddleCenter, true);
            Txt(ui, def.Name, x + 40, y + 136, 44, true, Ink);
            Label(ui, def.Desc, x + 40, y + 158, w - 80, 110, 20, false, Hex("#3f2b27"), TextAnchor.UpperLeft, true, true);

            const float bx = x + 40, by = y + 282, bw = 300, bh = 150;
            Panel(ui, bx, by, bw, bh, Border, Hex("#2b1d1c"), false);
            holdFrame = Box(ui, "Hold demo", 0, 0, 10, 10);
            holdEdges = new[]
            {
                Stretch(Rect(holdFrame, 0, 0, 0, 4, Color.white), new Vector2(0, 1), new Vector2(1, 1)),
                Stretch(Rect(holdFrame, 0, 0, 0, 4, Color.white), new Vector2(0, 0), new Vector2(1, 0)),
                Stretch(Rect(holdFrame, 0, 0, 4, 0, Color.white), new Vector2(0, 0), new Vector2(0, 1)),
                Stretch(Rect(holdFrame, 0, 0, 4, 0, Color.white), new Vector2(1, 0), new Vector2(1, 1))
            };
            trail = new RectTransform[6];
            for (int i = 0; i < trail.Length; i++)
                trail[i] = Rect(ui, 0, 0, 6, 6, Rgba(255, 192, 90, .5f - i * .07f)).rectTransform;
            hand = Raw(ui, "Demo hand", 0, 0, 48, 52, handTexture).rectTransform;

            Txt(ui, "조작", bx + bw + 36, by + 30, 16, false, Crimson, bold: true);
            Label(ui, def.Hint, bx + bw + 36, by + 44, 280, 76, 23, true, Ink, TextAnchor.UpperLeft, wrap: true);
            Txt(ui, "키보드·패드는 확인 키로도 돼요", bx + bw + 36, by + bh - 30, 15, false, Hex("#6b4c3c"), bold: true);
            Txt(ui, $"제한 시간 {def.Time}초", bx + bw + 36, by + bh - 6, 16, false, Hex("#5a4038"), bold: true);
            Label(ui, "회상 · " + def.Clue, x + 40, y + h - 88, 380, 70, 16, false, Hex("#6b4c3c"), TextAnchor.UpperLeft, true, true);

            Button start = Btn(ui, x + w - 250, y + h - 86, 210, 64, "시작!", StartPlay, size: 26);
            Focus(start.gameObject);
            AnimateDemo();
        }

        private void BuildPlay()
        {
            BuildHud(true);
            playPanel = Box(ui, "Play panel", 0, 0, ScreenW, ScreenH);
            Panel(playPanel, 316, 642, 648, 60, Border, PanelFill);
            Raw(playPanel, "Hint hand", 356 - 3.5f * 2.4f, 662 - 2.4f, 12 * 2.4f, 13 * 2.4f, handTexture);
            TextMid(playPanel, def.Hint, 660, 673, 22, false, Cream, TextAnchor.MiddleCenter, true);
            shout = TextMid(playPanel, "", 560, 170, 40, true, Cream, TextAnchor.MiddleCenter);
            Outline(shout, Stroke, 4);
            shout.gameObject.SetActive(false);

            readyRect = Box(ui, "Ready", 640 - 400, 330 - 80, 800, 160);
            readyText = Label(readyRect, "준비…", 0, 0, 800, 160, 96, true, Cream, TextAnchor.MiddleCenter);
            Outline(readyText, Stroke, 7);
            readyRect.pivot = new Vector2(.5f, .5f);
            readyRect.anchoredPosition = new Vector2(640, -330);
        }

        private void BuildGraded(int score, string note, Grade grade, string measure)
        {
            gradedRoot = Box(ui, "Graded", 0, 0, ScreenW, ScreenH);
            gradedDim = Rect(gradedRoot, 0, 100, ScreenW, ScreenH - 100, Rgba(18, 10, 12, 0));
            stampRect = Box(gradedRoot, "Stamp", 640 - 450, 290 - 100, 900, 200);
            stampRect.pivot = new Vector2(.5f, .5f);
            stampRect.anchoredPosition = new Vector2(640, -290);
            stampRect.localRotation = Quaternion.Euler(0, 0, 4);
            stampText = Label(stampRect, grade.Label, 0, 0, 900, 200, 120, true, grade.Color, TextAnchor.MiddleCenter);
            Outline(stampText, Hex("#2a1719"), 9);

            gradedDetails = Box(gradedRoot, "Details", 0, 0, ScreenW, ScreenH);
            Text scoreText = TextMid(gradedDetails, $"{score}점", 640, 380, 36, false, Cream, TextAnchor.MiddleCenter, true);
            Outline(scoreText, Stroke, 4);
            if (!string.IsNullOrEmpty(measure))
            {
                Text amounts = TextMid(gradedDetails, measure, 640, 432, 22, false, Hex("#FFD37A"), TextAnchor.MiddleCenter, true);
                Outline(amounts, Stroke, 3);
            }

            Panel(gradedDetails, 28, 512, 166, 166, Edge, Hex("#3b2621"));
            RawImage portrait = Raw(gradedDetails, "Hartz", 36, 520, 150, 150, hartz);
            portrait.uvRect = new UnityEngine.Rect(128 / 418f, (418 - 10 - 170) / 418f, 170 / 418f, 170 / 418f);
            portrait.enabled = hartz != null;
            Parchment(gradedDetails, 206, 528, 660, 150);
            Txt(gradedDetails, "하츠", 240, 554, 14, false, Crimson, bold: true);
            Txt(gradedDetails, grade.Line, 240, 590, 26, true, Ink);
            Txt(gradedDetails, note ?? "", 240, 636, 19, false, Hex("#5a4038"), bold: true);
            string nextLabel = step + 1 < steps.Length ? "다음 단계" : isUdon ? "완성된 우동 보기" : "완성된 감자전 보기";
            nextButton = Btn(gradedDetails, 930, 600, 300, 76, nextLabel, Next, size: 24);
            gradedDetails.gameObject.SetActive(false);
        }

        private void BuildResult()
        {
            Panel(ui, 240, 30, 400, 70, Hex("#E2A47C"), Crimson);
            TextMid(ui, "요리 완성!", 440, 66, 34, true, Hex("#FFD37A"), TextAnchor.MiddleCenter);

            const float x = 740, y = 30, w = 500, h = 580;
            Parchment(ui, x, y, w, h);
            Texture2D medal = KitchenArt.MedalSprite(tier).ToTexture();
            sceneAssets.Add(medal);
            Raw(ui, "Medal", x + 34, y + 34, 72, 96, medal);
            Txt(ui, isUdon ? "겐로쿠 우동" : Dishes[tier], x + 124, y + 72, 28, true, Ink);
            Txt(ui, $"총점 {total}점", x + 124, y + 104, 20, false, Crimson, bold: true);
            Batch made = k.Amounts;
            string amounts = isUdon ? $"물 {k.Udon.Water:0}ml · 쯔유 {k.Udon.Sauce:0}ml · 후추 {k.Udon.Pepper}번"
                : $"감자 1알 {Batch.Potato:0}g · 지름 {made.Diameter:0.0}cm · 두께 {made.Thickness:0.0}mm";
            Txt(ui, amounts, x + 124, y + 132, 15, false, Hex("#5a4038"), bold: true);
            for (float i = 0; i < w - 68; i += 10)
                Rect(ui, x + 34 + i, y + 150, 6, 2, Hex("#C9A878"));

            for (int i = 0; i < steps.Length; i++)
            {
                int score = Mathf.Max(0, scores[i]);
                Grade grade = GradeOf(score);
                float ry = y + 184 + i * 44;
                Txt(ui, $"{i + 1}. {steps[i].Name}", x + 36, ry, 18, false, Ink, bold: true);
                Panel(ui, x + w - 176, ry - 24, 104, 34, grade.Chip, grade.Chip, false, 2);
                TextMid(ui, grade.Label, x + w - 124, ry - 7, 15, false, Hex("#fff6e2"), TextAnchor.MiddleCenter, true);
                TextMid(ui, score.ToString(), x + w - 36, ry - 7, 17, false, Hex("#5a4038"), TextAnchor.MiddleRight, true);
            }

            Button serve = Btn(ui, x + 34, y + h - 96, 262, 64, "에이미에게 대접하기", () => served?.Invoke(tier), size: 21);
            Btn(ui, x + 308, y + h - 96, 158, 64, "다시 만들기", Retry, false, 19, false, false);

            const float dx = 32, dy = 604, dw = 690, dh = 100;
            Panel(ui, dx, dy, dw, dh, Edge, PanelFill, line: Rgba(140, 96, 66, .55f));
            Panel(ui, dx + 36, dy - 28, 100, 48, Edge, Crimson, false);
            TextMid(ui, "하츠", dx + 86, dy - 3, 24, true, Gold, TextAnchor.MiddleCenter);
            typedLine = isUdon ? (tier < 2 ? "대파 향이랑 후추가 딱 좋아. 식기 전에 가져가자." : "다음에는 간을 더 꼼꼼히 봐야겠어.") : HartzVerdict[tier];
            typedText = Label(ui, "", dx + 52, dy + 34, dw - 100, 50, 22, false, Cream, TextAnchor.UpperLeft, wrap: true);
            Focus(serve.gameObject);
        }

        private void ClearScreen()
        {
            if (ui != null)
                for (int i = ui.childCount - 1; i >= 0; i--)
                    Discard(ui.GetChild(i).gameObject);
            foreach (UnityEngine.Object asset in sceneAssets)
                Discard(asset);
            sceneAssets.Clear();
            gaugeViews.Clear();
            timerFill = null;
            typedText = null;
            queued.Clear();
        }

        private static void Discard(UnityEngine.Object target)
        {
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        // ================================================================== grades
        private struct Grade
        {
            public string Label, Line;
            public Color Color, Chip;
            public Sfx Sound;
        }

        private static Grade GradeOf(int s)
        {
            if (s >= 90)
                return new Grade { Label = "완벽!", Color = Hex("#FFC05A"), Chip = Hex("#C98A2E"), Line = "좋아, 그때 그 손맛이야!", Sound = Sfx.Perfect };
            if (s >= 70)
                return new Grade { Label = "좋아요!", Color = Hex("#a8dcb0"), Chip = Hex("#4f7f63"), Line = "이 정도면 에이미도 좋아하겠지.", Sound = Sfx.Ok };
            if (s >= 45)
                return new Grade { Label = "괜찮아요", Color = Hex("#f2b98a"), Chip = Hex("#9a5f2e"), Line = "음… 다음엔 조금 더 신경 쓰자.", Sound = Sfx.Ok };
            return new Grade { Label = "아쉬워요…", Color = Hex("#e3aaa4"), Chip = Hex("#5c4142"), Line = "괜찮아, 아직 만회할 수 있어.", Sound = Sfx.Bad };
        }

        // ================================================================== side gauges
        private sealed class GaugeView
        {
            private const float W = 290, H = 104, TrackX = 24, TrackY = 52, TrackW = W - 48, TrackH = 16;
            public readonly RectTransform Root;
            private readonly Text label, value, zoneLabel;
            private readonly Image[] segments = new Image[10];
            private readonly Image fill, zone, zoneStart, zoneEnd, marker, cap;
            private readonly RawImage gradient;

            public GaugeView(PotatoKitchen kitchen, RectTransform parent, float x, float y)
            {
                Root = kitchen.Box(parent, "Gauge", x, y, W, H);
                kitchen.Panel(Root, 0, 0, W, H, Border, PanelFill);
                label = kitchen.Txt(Root, "", 24, 36, 17, false, Muted);
                value = kitchen.Txt(Root, "", W - 24, 36, 19, false, Cream, TextAnchor.MiddleRight, true);
                kitchen.Rect(Root, TrackX, TrackY, TrackW, TrackH, Dark);
                gradient = kitchen.Raw(Root, "Cook colours", TrackX, TrackY, TrackW, TrackH, cookGradient);
                float pitch = (TrackW - 4) / segments.Length;
                for (int i = 0; i < segments.Length; i++)
                {
                    segments[i] = kitchen.Rect(Root, TrackX + 2 + i * pitch, TrackY + 2, pitch - 2, TrackH - 4, Color.white);
                    Stretch(kitchen.Rect(segments[i].rectTransform, 0, 0, 0, 3, Rgba(255, 255, 255, .25f)), new Vector2(0, 1), new Vector2(1, 1));
                }

                fill = kitchen.Rect(Root, TrackX + 2, TrackY + 2, 0, TrackH - 4, Color.white);
                Stretch(kitchen.Rect(fill.rectTransform, 0, 0, 0, 3, Rgba(255, 255, 255, .3f)), new Vector2(0, 1), new Vector2(1, 1));
                zone = kitchen.Rect(Root, 0, TrackY, 0, TrackH, Color.white);
                zoneStart = kitchen.Rect(Root, 0, TrackY - 6, 3, TrackH + 12, Color.white);
                zoneEnd = kitchen.Rect(Root, 0, TrackY - 6, 3, TrackH + 12, Color.white);
                zoneLabel = kitchen.TextMid(Root, "", 0, 86, 14, false, Color.white, TextAnchor.MiddleCenter, true);
                marker = kitchen.Rect(Root, 0, TrackY - 8, 4, TrackH + 16, Cream);
                cap = kitchen.Rect(Root, 0, TrackY - 12, 14, 4, Cream);
            }

            public void Set(Gauge g)
            {
                label.text = g.Label;
                value.text = g.Value ?? "";
                gradient.gameObject.SetActive(g.CookGradient);

                float f = Clamp(g.Fill, 0, 1);
                for (int i = 0; i < segments.Length; i++)
                {
                    bool on = g.Segmented && (i + 1) / (float)segments.Length <= f + .001f;
                    segments[i].gameObject.SetActive(on);
                    segments[i].color = g.FillColor;
                }

                fill.gameObject.SetActive(!g.Segmented && g.Fill >= 0);
                fill.color = g.FillColor;
                fill.rectTransform.sizeDelta = new Vector2((TrackW - 4) * f, TrackH - 4);

                bool hasZone = g.ZoneMin >= 0;
                zone.gameObject.SetActive(hasZone);
                zoneStart.gameObject.SetActive(hasZone);
                zoneEnd.gameObject.SetActive(hasZone);
                zoneLabel.gameObject.SetActive(hasZone && !string.IsNullOrEmpty(g.ZoneLabel));
                if (hasZone)
                {
                    float z0 = TrackX + TrackW * g.ZoneMin, z1 = TrackX + TrackW * g.ZoneMax;
                    Color strong = Color.Lerp(new Color32(g.ZoneColor.r, g.ZoneColor.g, g.ZoneColor.b, 255), Color.white, .3f);
                    zone.color = g.ZoneColor;
                    Place(zone.rectTransform, z0, TrackY, z1 - z0, TrackH);
                    zoneStart.color = zoneEnd.color = strong;
                    Place(zoneStart.rectTransform, z0, TrackY - 6, 3, TrackH + 12);
                    Place(zoneEnd.rectTransform, z1 - 3, TrackY - 6, 3, TrackH + 12);
                    zoneLabel.text = g.ZoneLabel;
                    zoneLabel.color = strong;
                    Place(zoneLabel.rectTransform, (z0 + z1) / 2 - 500, 86 - 14, 1000, 28);
                }

                bool hasMarker = g.Marker >= 0;
                marker.gameObject.SetActive(hasMarker);
                cap.gameObject.SetActive(hasMarker && g.CookGradient);
                if (hasMarker)
                {
                    float mx = TrackX + TrackW * Clamp(g.Marker, 0, 1);
                    marker.color = g.MarkerAlert ? Hex("#e07a6a") : g.CookGradient ? Cream : Gold;
                    Place(marker.rectTransform, mx - 2, TrackY - 8, 4, TrackH + 16);
                    Place(cap.rectTransform, mx - 7, TrackY - 12, 14, 4);
                }
            }
        }

        // ================================================================== uGUI helpers (top-left, y-down coordinates)
        private RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            Place(rect, x, y, w, h);
            return rect;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            Vector2 pivot = rect.pivot;
            rect.anchoredPosition = new Vector2(x + w * pivot.x, -(y + h * (1 - pivot.y)));
            rect.sizeDelta = new Vector2(w, h);
        }

        private static Image Stretch(Image image, Vector2 min, Vector2 max)
        {
            RectTransform rect = image.rectTransform;
            Vector2 size = rect.sizeDelta;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(min.x == max.x ? (min.x == 0 ? size.x / 2 : -size.x / 2) : 0,
                min.y == max.y ? (min.y == 0 ? size.y / 2 : -size.y / 2) : 0);
            rect.sizeDelta = new Vector2(min.x == max.x ? size.x : 0, min.y == max.y ? size.y : 0);
            return image;
        }

        private Image Rect(Transform parent, float x, float y, float w, float h, Color color)
        {
            Image image = Box(parent, "Rect", x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private RawImage Raw(Transform parent, string name, float x, float y, float w, float h, Texture texture)
        {
            RawImage image = Box(parent, name, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private Image Stepped(Transform parent, float x, float y, float w, float h, Color color, int s)
        {
            Image image = Rect(parent, x, y, w, h, color);
            image.sprite = stepped;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f / s;
            return image;
        }

        /// <summary>Stepped-corner pixel panel: drop shadow, coloured edge, inner fill.</summary>
        private Image Panel(Transform parent, float x, float y, float w, float h, Color edge, Color fill, bool shadow = true, int s = 4,
            Color? line = null, Color? shine = null)
        {
            if (shadow)
                Stepped(parent, x, y + 8, w, h, new Color(0, 0, 0, .35f), s);
            Stepped(parent, x, y, w, h, edge, s);
            Image inner = Stepped(parent, x + s, y + s, w - 2 * s, h - 2 * s, fill, s);
            if (line.HasValue)
            {
                Color c = line.Value;
                Rect(parent, x + 3 * s, y + 3 * s, w - 6 * s, 2, c);
                Rect(parent, x + 3 * s, y + h - 3 * s - 2, w - 6 * s, 2, c);
                Rect(parent, x + 3 * s, y + 3 * s, 2, h - 6 * s, c);
                Rect(parent, x + w - 3 * s - 2, y + 3 * s, 2, h - 6 * s, c);
            }

            if (shine.HasValue)
                Rect(parent, x + 3 * s, y + 2 * s, w - 6 * s, s, shine.Value);
            return inner;
        }

        private void Parchment(Transform parent, float x, float y, float w, float h) =>
            Panel(parent, x, y, w, h, Border, Hex("#F2DFBA"), line: Hex("#D9B98A"));

        private Text Label(Transform parent, string value, float x, float y, float w, float h, int size, bool heading, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
        {
            Text text = Box(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
            text.font = heading ? headingFont : bodyFont;
            text.fontSize = size;
            text.fontStyle = bold && !heading ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = wrap ? 1.1f : 1;
            text.text = value;
            return text;
        }

        /// <summary>Text placed by its baseline, like canvas fillText.</summary>
        private Text Txt(Transform parent, string value, float x, float baseline, int size, bool heading, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false) =>
            TextMid(parent, value, x, baseline - size * .36f, size, heading, color, anchor, bold);

        /// <summary>Text placed by its vertical middle; x is the left, centre or right edge depending on the anchor.</summary>
        private Text TextMid(Transform parent, string value, float x, float middle, int size, bool heading, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            const float width = 1000;
            float left = anchor == TextAnchor.MiddleCenter ? x - width / 2 : anchor == TextAnchor.MiddleRight ? x - width : x;
            return Label(parent, value, left, middle - size, width, size * 2, size, heading, color, anchor, bold);
        }

        private static void Outline(Text text, Color color, float width)
        {
            var outer = text.gameObject.AddComponent<Outline>();
            outer.effectColor = color;
            outer.effectDistance = new Vector2(width, -width);
            var inner = text.gameObject.AddComponent<Outline>();
            inner.effectColor = color;
            inner.effectDistance = new Vector2(width / 2, -width / 2);
        }

        private Button Btn(Transform parent, float x, float y, float w, float h, string label, UnityAction onClick,
            bool primary = true, int size = 22, bool heading = true, bool arrow = true)
        {
            Stepped(parent, x, y + 8, w, h, new Color(0, 0, 0, .35f), 4);
            Stepped(parent, x, y, w, h, primary ? Hex("#E2A47C") : Border, 4);
            Image face = Stepped(parent, x + 4, y + 4, w - 8, h - 8, Color.white, 4);
            face.raycastTarget = true;
            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.normalColor = primary ? Crimson : Hex("#2e1d1e");
            colors.highlightedColor = colors.selectedColor = primary ? Hex("#93353A") : Hex("#4a302a");
            colors.pressedColor = primary ? Hex("#5c1f24") : Hex("#1f1415");
            colors.fadeDuration = .06f;
            button.colors = colors;
            button.onClick.AddListener(() => k.Play(Sfx.Tap));
            button.onClick.AddListener(onClick);
            Rect(parent, x + 12, y + 8, w - 24, 4, primary ? Rgba(255, 200, 160, .22f) : Rgba(255, 235, 199, .06f));

            float textX = x + w / 2;
            if (primary && arrow)
            {
                float ax = x + 28, ay = y + h / 2 - 7;
                for (int i = 0; i < 4; i++)
                    Rect(parent, ax + i * 3, ay + i * 2, 3, 14 - i * 4, Gold);
                textX += 10;
            }

            TextMid(parent, label, textX, y + h / 2 + 1, size, heading, Cream, TextAnchor.MiddleCenter);
            return button;
        }

        private static void Focus(GameObject target)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return;
            if (events.currentSelectedGameObject == target)
                events.SetSelectedGameObject(null);
            events.SetSelectedGameObject(target);
        }

        private static void BuildStaticTextures()
        {
            if (stepped == null)
            {
                // 17 × 17 white tile with 4 px stair-stepped corners, sliced 8 px in from each side.
                var tile = new Texture2D(17, 17, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[17 * 17];
                for (int y = 0; y < 17; y++)
                for (int x = 0; x < 17; x++)
                {
                    int dx = Math.Min(x, 16 - x), dy = Math.Min(y, 16 - y);
                    bool cut = (dx < 4 && dy < 8) || (dx < 8 && dy < 4);
                    px[y * 17 + x] = cut ? new Color32(255, 255, 255, 0) : new Color32(255, 255, 255, 255);
                }

                tile.SetPixels32(px);
                tile.Apply(false);
                stepped = Sprite.Create(tile, new UnityEngine.Rect(0, 0, 17, 17), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
            }

            if (cookGradient == null)
            {
                const int n = 26;
                cookGradient = new Texture2D(n, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n];
                for (int i = 0; i < n; i++)
                    px[i] = KitchenArt.CookColor(i / (n - 1f) * 1.2f);
                cookGradient.SetPixels32(px);
                cookGradient.Apply(false);
            }

            if (vignette == null)
            {
                const int w = 160, h = 90;
                vignette = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[w * h];
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sx = (x + .5f) * 8, sy = ScreenH - (y + .5f) * 8;
                    float d = Mathf.Sqrt(Sq(sx - 640) + Sq(sy - 350));
                    px[y * w + x] = Rgba(15, 8, 9, .6f * Clamp((d - 252) / 432, 0, 1));
                }

                vignette.SetPixels32(px);
                vignette.Apply(false);
            }

            if (handTexture == null)
                handTexture = KitchenArt.Hand.ToTexture();
        }
    }
}
