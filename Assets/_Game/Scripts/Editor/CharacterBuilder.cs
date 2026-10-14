using System.IO;
using Biskas.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Biskas.EditorTools
{
    /// <summary>
    /// Собирает анимированного персонажа (Бискас/Врискас) из частей-спрайтов (UI Image).
    /// Части лежат в Assets/_Game/Art/Characters/{Folder}/{prefix}_{part}.png на общем холсте 512×512.
    /// </summary>
    public static class CharacterBuilder
    {
        public enum Who { Biskas, Vriskas }

        // Суставы в координатах общего холста 512×512 (Unity: y снизу).
        private static readonly Vector2 TailPivot = new Vector2(318f / 512f, 1f - 440f / 512f);
        private static readonly Vector2 BodyPivot = new Vector2(0.5f, 1f - 486f / 512f);
        private static readonly Vector2 HeadPivot = new Vector2(0.5f, 1f - 290f / 512f);

        private static string Prefix(Who who) => who == Who.Biskas ? "biskas" : "vriskas";
        private static string Folder(Who who) => ArtImportSettings.ArtRoot + "Characters/" + who + "/";

        public static CharacterAnimator Create(Transform parent, string name, Who who)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.SetParent(parent, false);
            SetUiLayer(root);

            string p = Prefix(who), dir = Folder(who);
            var tail = Part(rt, "Tail", dir, p + "_tail", TailPivot);
            var body = Part(rt, "Body", dir, p + "_body", BodyPivot);
            var head = Part(rt, "Head", dir, p + "_head", HeadPivot);
            var eyesOpen = Part(head, "EyesOpen", dir, p + "_eyes_open", new Vector2(0.5f, 0.5f));
            var eyesClosed = Part(head, "EyesClosed", dir, p + "_eyes_closed", new Vector2(0.5f, 0.5f));
            var mouth = Part(head, "MouthTalk", dir, p + "_mouth_talk", new Vector2(0.5f, 0.5f));
            var glow = Part(head, "EyesGlow", dir, p + "_eyes_glow", new Vector2(0.5f, 0.5f));
            eyesClosed.gameObject.SetActive(false);
            mouth.gameObject.SetActive(false);
            glow.gameObject.SetActive(false);

            var anim = root.AddComponent<CharacterAnimator>();
            var so = new SerializedObject(anim);
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("head").objectReferenceValue = head;
            so.FindProperty("tail").objectReferenceValue = tail;
            so.FindProperty("eyesOpen").objectReferenceValue = eyesOpen.gameObject;
            so.FindProperty("eyesClosed").objectReferenceValue = eyesClosed.gameObject;
            so.FindProperty("mouthTalk").objectReferenceValue = mouth.gameObject;
            so.FindProperty("eyesGlow").objectReferenceValue = glow.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return anim;
        }

        /// <summary>Персонаж в комнате (не перехватывает касания).</summary>
        public static CharacterAnimator CreateInRoom(Transform room, Who who, Vector2 anchor, float size)
        {
            var anim = Create(room, who + "_Room", who);
            var rt = (RectTransform)anim.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            return anim;
        }

        /// <summary>
        /// Персонаж, вписанный в произвольный прямоугольник (портрет, окно памяти).
        /// Части выровнены на квадратном холсте, а Image.preserveAspect прижимает спрайт к pivot,
        /// поэтому корень обязан быть квадратным (иначе голова «отрывается» от тела).
        /// </summary>
        public static CharacterAnimator CreateFitted(RectTransform container, string name, Who who, bool faceLeft)
        {
            var anim = Create(container, name, who);
            var fitter = anim.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            if (faceLeft) anim.transform.localScale = new Vector3(-1f, 1f, 1f);
            return anim;
        }

        private static RectTransform Part(Transform parent, string name, string dir, string sprite, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SetUiLayer(go);
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = pivot;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            string path = dir + sprite + ".png";
            img.sprite = ArtImportSettings.LoadSprite(path);
            if (img.sprite == null) throw new FileNotFoundException("Нет спрайта " + path);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return rt;
        }

        private static void SetUiLayer(GameObject go)
        {
            int ui = LayerMask.NameToLayer("UI");
            if (ui >= 0) go.layer = ui;
        }
    }
}
