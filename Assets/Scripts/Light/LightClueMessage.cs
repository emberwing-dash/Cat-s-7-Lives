using UnityEngine;

namespace CatOnTheSill
{
    /// <summary>Reveals a short environmental clue when the surface is lit or shadowed at the intended hour.</summary>
    public class LightClueMessage : MonoBehaviour
    {
        public string message = "FOLLOW THE LIGHT";
        public bool revealInShadow;
        public bool moonOnly;
        public bool sunOnly;
        public Vector3 surfaceNormal = Vector3.up;
        public Color hiddenColor = new Color(0.28f, 0.18f, 0.08f, 0.48f);
        public Color revealedColor = new Color(1f, 0.78f, 0.28f, 0.95f);
        public float fontSize = 0.18f;

        TextMesh textMesh;
        float visibility;

        // wall-text clues are retired (hints are pictorial); already-baked ones remove themselves on play
        void Awake()
        {
            if (Application.isPlaying) { Destroy(gameObject); return; }
            var textObject = new GameObject("HiddenMessage");
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = surfaceNormal.normalized * 0.018f;
            textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = message;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = fontSize;
            textMesh.color = hiddenColor;

            Vector3 n = surfaceNormal.normalized;
            textObject.transform.rotation = Quaternion.LookRotation(-n, Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up);
        }

        void Update()
        {
            if (textMesh == null) return;
            bool rightLight = (!moonOnly || SunLight.IsMoon) && (!sunOnly || !SunLight.IsMoon);
            bool visible = rightLight && (revealInShadow ? !SunLight.IsLit(transform.position, surfaceNormal) : SunLight.IsLit(transform.position, surfaceNormal));
            visibility = Mathf.MoveTowards(visibility, visible ? 1f : 0f, Time.deltaTime * 1.5f);
            textMesh.color = Color.Lerp(hiddenColor, revealedColor, visibility);
        }
    }
}

