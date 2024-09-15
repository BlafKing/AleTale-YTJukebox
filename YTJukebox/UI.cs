using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YTJukeboxMod {
    static internal class UI {
        static private GameObject JukeBoxControls;
        static public GameObject Youtube, Jukebox, GameCanvas;
        static public GameMenu gameMenu;
        static private TMP_InputField inputField;

        static private Sprite CreateSprite(string filePath) {
            Texture2D texture = LoadTextureFromFile(filePath);
            return texture != null ? Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f)) : null;
        }

        static private Texture2D LoadTextureFromFile(string filePath) {
            if (!File.Exists(filePath)) return null;
            Texture2D texture = new Texture2D(2, 2);
            return texture.LoadImage(File.ReadAllBytes(filePath)) ? texture : null;
        }

        static public void CreateCustomUI() {
            Youtube = new GameObject("YoutubeUI");
            Jukebox = GameObject.Find("Common/GameCanvas/Jukebox Screen");
            GameCanvas = GameObject.Find("Common/GameCanvas");
            JukeBoxControls = GameObject.Find("Common/GameCanvas/Jukebox Screen/Panel/Controls");
            gameMenu = GameCanvas.GetComponent<GameMenu>();
            CreateYTButton();
            CreateYTUI();
        }

        static private void CreateYTButton() {
            GameObject YtbButton = new GameObject("YtbButton");
            YtbButton.transform.SetParent(JukeBoxControls.transform, false);

            Image ButtonImage = YtbButton.AddComponent<Image>();
            ButtonImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "btn_rectangle_01_n_bluegray");
            ButtonImage.type = Image.Type.Sliced;

            YtbButton.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);

            GameObject YtbIcon = new GameObject("Image");
            YtbIcon.transform.SetParent(YtbButton.transform, false);
            Image YtbImage = YtbIcon.AddComponent<Image>();
            YtbImage.sprite = CreateSprite(Path.Combine(ModPaths.dependencies, "ytbIcon.png"));
            YtbImage.color = new Color(0.1451f, 0.1137f, 0.1529f, 1f);
            YtbIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 28);
            YtbIcon.GetComponent<RectTransform>().localPosition = Vector3.zero;

            Button button = YtbButton.AddComponent<Button>();
            button.onClick.AddListener(() => {
                Jukebox.SetActive(false);
                Youtube.SetActive(true);
                gameMenu.enabled = false;
            });
        }

        static private void CreateYTUI() {
            Youtube.transform.SetParent(GameCanvas.transform, false);
            Youtube.SetActive(false);

            Image YoutubeImage = Youtube.AddComponent<Image>();
            YoutubeImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "frame_stageframe_02_d");
            YoutubeImage.type = Image.Type.Sliced;
            Youtube.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 300);

            GameObject FrameUI = new GameObject("Frame");
            FrameUI.transform.SetParent(Youtube.transform, false);
            Image FrameImage = FrameUI.AddComponent<Image>();
            FrameImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "frame_listframe_01_n");
            FrameImage.type = Image.Type.Sliced;
            FrameUI.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 260);

            GameObject MainContainer = new GameObject("MainContainer");
            MainContainer.transform.SetParent(FrameUI.transform, false);

            CreateInputField(MainContainer);
            CreatePlayButton(MainContainer);
        }

        static private void CreateInputField(GameObject parent) {
            GameObject InputField = new GameObject("URLInputField");
            InputField.transform.SetParent(parent.transform, false);
            InputField.transform.localPosition = new Vector3(0, 50, 0);

            Image InputBackground = InputField.AddComponent<Image>();
            InputBackground.sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "textfield_activity_0");
            InputBackground.type = Image.Type.Sliced;

            RectTransform InputTransform = InputField.GetComponent<RectTransform>();
            InputTransform.sizeDelta = new Vector2(700, 40);

            GameObject TextObject = new GameObject("InputText");
            TextObject.transform.SetParent(InputField.transform, false);
            TextMeshProUGUI textMeshPro = TextObject.AddComponent<TextMeshProUGUI>();
            textMeshPro.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "Lato-Regular SDF");
            textMeshPro.fontSize = 24;
            textMeshPro.color = Color.white;
            textMeshPro.alignment = TextAlignmentOptions.Center;

            RectTransform TextTransform = TextObject.GetComponent<RectTransform>();
            TextTransform.sizeDelta = new Vector2(680, 30);
            TextTransform.localPosition = Vector3.zero;

            inputField = InputField.AddComponent<TMP_InputField>();
            inputField.textViewport = InputTransform;
            inputField.textComponent = textMeshPro;

            GameObject PlaceholderObject = new GameObject("PlaceholderText");
            PlaceholderObject.transform.SetParent(InputField.transform, false);
            TextMeshProUGUI placeholderText = PlaceholderObject.AddComponent<TextMeshProUGUI>();
            placeholderText.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "Lato-Regular SDF");
            placeholderText.fontSize = 24;
            placeholderText.color = Color.gray;
            placeholderText.alignment = TextAlignmentOptions.Center;
            placeholderText.text = "Enter URL here...";

            PlaceholderObject.GetComponent<RectTransform>().sizeDelta = new Vector2(680, 30);
            PlaceholderObject.GetComponent<RectTransform>().localPosition = Vector3.zero;

            inputField.placeholder = placeholderText;
        }

        static private void CreatePlayButton(GameObject parent) {
            GameObject PlayButton = new GameObject("PlayButton");
            PlayButton.transform.SetParent(parent.transform, false);
            PlayButton.transform.localPosition = new Vector3(0, -20, 0);

            Image ButtonImage = PlayButton.AddComponent<Image>();
            ButtonImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(s => s.name == "btn_rectangle_01_n_bluegray");
            ButtonImage.type = Image.Type.Sliced;

            PlayButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 60);

            GameObject PlayTextObject = new GameObject("PlayText");
            PlayTextObject.transform.SetParent(PlayButton.transform, false);
            TextMeshProUGUI buttonText = PlayTextObject.AddComponent<TextMeshProUGUI>();
            buttonText.font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == "Lato-Regular SDF");
            buttonText.fontSize = 30;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.text = "Play";

            RectTransform PlayTextTransform = PlayTextObject.GetComponent<RectTransform>();
            PlayTextTransform.sizeDelta = new Vector2(120, 60);
            PlayTextTransform.localPosition = Vector3.zero;
            PlayTextTransform.anchorMax = new Vector2(0.5f, 0.6f);

            Button button = PlayButton.AddComponent<Button>();
            button.onClick.AddListener(async () => {
                await Download.GetCustomSong(inputField.text);
            });
        }
    }
}
