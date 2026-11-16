using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FantasyClinaryArk
{
    public sealed class PotatoPancakeStage : MonoBehaviour
    {
        private const string RecipeKey = "Recipes.GoldenPotatoPancake";
        private const string Dish = "산골 마을의 황금 감자전";
        private const string UdonRecipeKey = "Recipes.GenrokuUdon";
        private const string UdonDish = "겐로쿠 우동";
        private const string UdonRecipe =
            "1. 대파를 3~4cm로 썰어 기름 없는 팬에 강불로 굽는다.\n" +
            "2. 겉이 살짝 그을리고 향이 올라오면 물 600ml를 붓는다.\n" +
            "3. 쯔유 50~100ml로 간하고 후추를 넉넉히 넣는다.\n" +
            "4. 후추의 매운맛이 올라오면 우동면을 넣어 익힌다.\n" +
            "선택 토핑 · 유부를 국물에 불리거나, 소고기 또는 닭다리살을 별도 팬에서 익혀 올린다.";
        // One medium potato makes one thin pancake about 14 cm across (≈90 g batter at 5–6 mm).
        private const string Ingredients = "재료 · 감자 1개(약 150g), 소금 한 꼬집(약 0.5g), 식용유 1큰술";
        private const string Recipe =
            "1. 감자 1개의 껍질을 벗겨(약 135g) 강판에 간다.\n" +
            "2. 면포로 꼭 짜서 물기(약 40ml)를 빼고, 감자 물은 5분쯤 둔다.\n" +
            "3. 윗물을 따라 내고 바닥의 전분(약 5g)과 소금 한 꼬집(약 0.5g)을 반죽에 섞는다.\n" +
            "4. 중불에 달군 팬에 식용유 1큰술을 두르고 반죽을 지름 14cm쯤, 5mm 두께로 얇게 편다.\n" +
            "5. 가장자리가 노릇해지고 굳으면 뒤집어 속까지 익힌다.";

        private enum Art { Rain = 0, Grater = 1, Plate = 7, Scallions, Udon }

        private readonly struct Line
        {
            public readonly string Speaker;
            public readonly string Text;

            public Line(string speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }
        }

        private static readonly Line[] Visit =
        {
            new("에이미", "너 예전에 비 맞고 쫄딱 젖었을 때 감자 부쳐 준 거 기억나?"),
            new("하츠", "아, 그때 돈 없어서 감자밖에 못 샀던 날?"),
            new("에이미", "그거 생각보다 맛있었잖아. 오늘도 해줄 수 있어?"),
            new("하츠", "감자만 있으면 되지. 잠깐만 기다려 봐.")
        };

        private static readonly Line[] Memory =
        {
            new("하츠의 기억", "의뢰를 마친 두 사람은 비를 피해 산골 마을의 작은 숙소에 들렀다. 부엌에 남은 건 감자 몇 알뿐이었다."),
            new("하츠의 기억", "처마에 빗물이 고였다. 체 아래에 받아 둔 감자 물도, 가만히 두니 바닥에 하얀 층이 생겼다."),
            new("에이미", "젖은 장갑을 말리는 동안, 팬에서 지글거리는 소리가 났어. 가장자리부터 노릇해지던 게 아직도 기억나.")
        };

        private static readonly Line[] UdonVisit =
        {
            new("에이미", "감자전도 좋았는데, 오늘은 뜨끈한 국물도 당기네. 예전에 나루터에서 먹은 우동 기억나?"),
            new("하츠", "후추 향이 코끝을 찔렀던 거? 그건 대파부터 구워야 해.")
        };

        private static readonly Line[] UdonMemory =
        {
            new("하츠의 기억", "막배를 기다리던 밤, 나루터의 요리사는 빈 팬에 대파를 올렸다. 기름을 두르지 않았는데도 향이 부두까지 퍼졌다."),
            new("에이미", "후추 들어가는 걸 보고 너무 많이 넣는 줄 알았어. 그런데 그게 자꾸 생각나는 맛이더라고.")
        };

        // Amy's reaction follows how well the pancake turned out (result tier 0 = best).
        private static readonly Line[][] Tasting =
        {
            new Line[]
            {
                new("에이미", "맞아, 이 맛이었어. 그때도 네가 이런 걸 해줬지."),
                new("하츠", "그때보다 바삭하잖아. 나도 그동안 발전은 했다고.")
            },
            new Line[]
            {
                new("에이미", "음, 고소하다. 그날 숙소에서 먹던 맛이랑 거의 똑같아."),
                new("하츠", "거의? 다음엔 완전히 똑같이 해 줄게.")
            },
            new Line[]
            {
                new("에이미", "조금 눅눅하긴 한데… 그래도 네가 해 준 거라 맛있어."),
                new("하츠", "…다음엔 꼭 바삭하게 부쳐 줄게.")
            },
            new Line[]
            {
                new("에이미", "하츠… 이건 좀 탄 것 같은데? 그래도 마음은 고마워."),
                new("하츠", "그, 그날도 이랬던 것 같은데…")
            }
        };

        private static readonly Color Gold = new Color32(237, 181, 111, 255);
        private static readonly Color Cream = new Color32(255, 235, 199, 255);
        private static readonly Color Border = new Color32(140, 96, 66, 255);
        private static readonly Color Panel = new Color32(37, 25, 26, 249);
        private static readonly Color Choice = new Color32(82, 52, 44, 255);

        [SerializeField] private Font headingFont;
        [SerializeField] private Font bodyFont;
        [SerializeField] private Texture2D artwork;
        [SerializeField] private Texture2D restaurant;
        [SerializeField] private Texture2D hartzPortrait;

        private Text chapter;
        private Text dishTitle;
        private Text speaker;
        private Text body;
        private Text caption;
        private RawImage illustration;
        private Button primary;
        private Button secondary;
        private Texture2D scallionArt, udonArt;
        private RectTransform header;
        private RectTransform story;
        private PotatoKitchen kitchen;
        private Button backButton;
        private UnityAction exitToTitle;

        public bool IsRecipeUnlocked => PlayerPrefs.GetInt(RecipeKey, 0) == 1;

        public void Begin(UnityAction returnToTitle)
        {
            Open(returnToTitle);
            ShowDialogue("식당 · 에이미의 방문", Art.Grater, "은퇴한 용사 하츠의 식당", Visit, 0,
                "기억 떠올리기", () => ShowDialogue("회상 · 비 오는 산골 마을", Art.Rain,
                    "산골 마을의 작은 숙소", Memory, 0, "감자전 만들기", StartCooking));
        }

        public void OpenRecipe(UnityAction returnToTitle)
        {
            Open(returnToTitle);
            ShowRecipe();
        }

        private void Open(UnityAction returnToTitle)
        {
            exitToTitle = returnToTitle;
            if (illustration == null)
                BuildView();

            gameObject.SetActive(true);
        }

        private void ShowDialogue(string section, Art art, string artCaption, Line[] lines, int index,
            string lastAction, UnityAction finished)
        {
            Line line = lines[index];
            SetPage(section, line.Speaker, line.Text, art, artCaption);
            if (index + 1 < lines.Length)
            {
                SetButton(primary, "다음", () =>
                    ShowDialogue(section, art, artCaption, lines, index + 1, lastAction, finished));
            }
            else
            {
                SetButton(primary, lastAction, finished);
            }
            Focus(primary);
        }

        private void StartCooking()
        {
            StartKitchen(Served, false);
        }

        private void StartKitchen(System.Action<int> served, bool udon)
        {
            header.gameObject.SetActive(false);
            story.gameObject.SetActive(false);
            kitchen.gameObject.SetActive(true);
            kitchen.Begin(served, () => exitToTitle?.Invoke(), udon);
        }

        private void Served(int tier)
        {
            ShowDialogue("식당 · 다시 만난 맛", Art.Plate, "에이미의 감자전",
                Tasting[Mathf.Clamp(tier, 0, Tasting.Length - 1)], 0, "레시피·판매 메뉴 등록", UnlockRecipe);
        }

        private void UnlockRecipe()
        {
            PlayerPrefs.SetInt(RecipeKey, 1);
            PlayerPrefs.Save();
            ShowRecipe();
        }

        private void ShowRecipe()
        {
            SetPage("식당 · 등록된 레시피", Ingredients, Recipe,
                Art.Plate, "판매 메뉴 해금");
            SetButton(primary, "1-2 겐로쿠 우동", BeginUdon);
            if (PlayerPrefs.GetInt(UdonRecipeKey, 0) == 1)
                SetButton(secondary, "우동 레시피", ShowUdonRecipe);
            Focus(primary);
        }

        private void BeginUdon()
        {
            ShowDialogue("식당 · 따뜻한 국물", Art.Scallions, "겐로쿠 우동",
                UdonVisit, 0, "기억 떠올리기", () =>
                    ShowDialogue("회상 · 막배를 기다리며", Art.Scallions, "나루터의 국물",
                        UdonMemory, 0, "우동 만들기", () => StartKitchen(ServeUdon, true)));
        }

        private void ServeUdon(int tier)
        {
            Line[] tasting =
            {
                new("에이미", tier < 2 ? "이 향이었어. 한 모금 마시니까 막배 기다리던 밤이 생각난다."
                    : "따뜻하다. 그날 먹었던 건 대파 향이랑 후추 맛이 조금 더 또렷했어."),
                new("하츠", tier < 2 ? "다음에 배 놓치면 여기로 와. 한 그릇 말아 줄게."
                    : "기억해 둘게. 다음에 오면 한 그릇 더 해 줄게.")
            };
            ShowDialogue("식당 · 나루터의 맛", Art.Udon, "에이미의 겐로쿠 우동", tasting, 0,
                "레시피·판매 메뉴 등록", UnlockUdon);
        }

        private void UnlockUdon()
        {
            PlayerPrefs.SetInt(UdonRecipeKey, 1);
            PlayerPrefs.Save();
            ShowUdonRecipe();
        }

        private void ShowUdonRecipe()
        {
            SetPage("식당 · 등록된 레시피", "재료 · 대파, 쯔유(혼쯔유 기준), 후추, 우동면", UdonRecipe,
                Art.Udon, "판매 메뉴 해금");
            SetButton(primary, "1-2 다시 만들기", BeginUdon);
            SetButton(secondary, "감자전 레시피", ShowRecipe);
            Focus(primary);
        }

        private void SetPage(string section, string name, string message, Art art, string artCaption)
        {
            chapter.text = section;
            speaker.text = name;
            body.text = message;
            caption.text = artCaption;
            kitchen.gameObject.SetActive(false);
            header.gameObject.SetActive(true);
            story.gameObject.SetActive(true);
            SetArt(art);
            primary.gameObject.SetActive(false);
            secondary.gameObject.SetActive(false);
        }

        private void SetArt(Art art)
        {
            if (art == Art.Scallions || art == Art.Udon)
            {
                if (art == Art.Scallions)
                {
                    if (scallionArt == null)
                        scallionArt = UdonArt.StorySprite(false).ToTexture();
                    illustration.texture = scallionArt;
                }
                else
                {
                    if (udonArt == null)
                        udonArt = UdonArt.StorySprite(true).ToTexture();
                    illustration.texture = udonArt;
                }
                dishTitle.text = UdonDish;
                illustration.uvRect = new Rect(.14f, 0, .72f, 1);
                return;
            }

            dishTitle.text = Dish;
            illustration.texture = artwork;
            int tile = (int)art;
            illustration.uvRect = new Rect((tile % 4) * .25f, (1 - tile / 4) * .5f, .25f, .5f);
        }

        private void OnDestroy()
        {
            if (scallionArt != null) Destroy(scallionArt);
            if (udonArt != null) Destroy(udonArt);
        }

        private void BuildView()
        {
            // Coordinates are in the existing title canvas's 1280 × 720 composition.
            var root = (RectTransform)transform;
            story = Place(root, "Narrative", new Rect(0, 0, 1280, 720));
            BuildStory(story);

            header = Place(root, "Header", new Rect(0, 0, 1280, 720));
            AddSurface(header, "Header edge", new Rect(64, 566, 1152, 104), Border);
            AddSurface(header, "Header", new Rect(66, 568, 1148, 100), Panel);
            dishTitle = AddText(header, "Dish title", new Rect(90, 580, 870, 53), Dish, headingFont, 43, Cream);
            chapter = AddText(header, "Chapter", new Rect(92, 632, 870, 28), "", bodyFont, 21, Gold);
            backButton = AddButton(header, "Back", new Rect(1024, 594, 162, 48));
            SetButton(backButton, "타이틀로", () => exitToTitle?.Invoke());

            RectTransform cooking = Place(root, "Kitchen", new Rect(0, 0, 1280, 720));
            kitchen = cooking.gameObject.AddComponent<PotatoKitchen>();
            kitchen.Setup(headingFont, bodyFont, restaurant, hartzPortrait);
            cooking.gameObject.SetActive(false);
        }

        private void BuildStory(RectTransform root)
        {
            AddSurface(root, "Illustration edge", new Rect(64, 98, 424, 438), Border);
            AddSurface(root, "Illustration panel", new Rect(66, 100, 420, 434), Panel);
            illustration = Place(root, "Scene art", new Rect(84, 220, 384, 300)).gameObject.AddComponent<RawImage>();
            illustration.texture = artwork;
            illustration.raycastTarget = false;
            caption = AddText(root, "Scene caption", new Rect(86, 154, 380, 62), "", bodyFont, 23, Cream,
                TextAnchor.MiddleCenter);

            AddSurface(root, "Dialogue edge", new Rect(512, 98, 704, 438), Border);
            AddSurface(root, "Dialogue panel", new Rect(514, 100, 700, 434), Panel);
            speaker = AddText(root, "Speaker or step", new Rect(544, 470, 638, 40), "", bodyFont, 27, Gold);
            AddSurface(root, "Dialogue rule", new Rect(544, 461, 638, 2), Border);
            body = AddText(root, "Dialogue or recipe", new Rect(544, 220, 638, 224), "", bodyFont, 30, Cream,
                TextAnchor.UpperLeft);
            primary = AddButton(root, "Advance", new Rect(866, 126, 316, 58));
            secondary = AddButton(root, "Other recipe", new Rect(544, 126, 300, 58));
        }

        private static RectTransform Place(RectTransform parent, string name, Rect bounds)
        {
            var child = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            child.SetParent(parent, false);
            child.anchorMin = new Vector2(bounds.xMin / 1280, bounds.yMin / 720);
            child.anchorMax = new Vector2(bounds.xMax / 1280, bounds.yMax / 720);
            child.offsetMin = Vector2.zero;
            child.offsetMax = Vector2.zero;
            return child;
        }

        private static Image AddSurface(RectTransform parent, string name, Rect bounds, Color color)
        {
            Image surface = Place(parent, name, bounds).gameObject.AddComponent<Image>();
            surface.color = color;
            surface.raycastTarget = false;
            return surface;
        }

        private static Text AddText(RectTransform parent, string name, Rect bounds, string value, Font font,
            int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            return AddText(Place(parent, name, bounds), value, font, size, color, alignment);
        }

        private static Text AddText(RectTransform rect, string value, Font font, int size, Color color,
            TextAnchor alignment)
        {
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 1;
            label.resizeTextMaxSize = size;
            label.color = color;
            label.alignment = alignment;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.text = value;
            return label;
        }

        private Button AddButton(RectTransform parent, string name, Rect bounds)
        {
            Image surface = AddSurface(parent, name, bounds, Color.white);
            surface.raycastTarget = true;
            Button button = surface.gameObject.AddComponent<Button>();
            button.targetGraphic = surface;
            ColorBlock colors = button.colors;
            colors.normalColor = Choice;
            colors.highlightedColor = new Color32(133, 83, 55, 255);
            colors.selectedColor = new Color32(133, 83, 55, 255);
            colors.pressedColor = new Color32(58, 34, 29, 255);
            button.colors = colors;

            var label = (RectTransform)new GameObject("Label", typeof(RectTransform)).transform;
            label.SetParent(surface.rectTransform, false);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.offsetMin = new Vector2(12, 3);
            label.offsetMax = new Vector2(-12, -3);
            AddText(label, "", bodyFont, 25, Cream, TextAnchor.MiddleCenter);
            return button;
        }

        private static void SetButton(Button button, string label, UnityAction action)
        {
            button.GetComponentInChildren<Text>(true).text = label;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            button.gameObject.SetActive(true);
        }

        private static void Focus(Button button) => Focus(button.gameObject);

        private static void Focus(GameObject target)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem.currentSelectedGameObject == target)
                eventSystem.SetSelectedGameObject(null);

            eventSystem.SetSelectedGameObject(target);
        }
    }
}
