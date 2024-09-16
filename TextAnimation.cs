#undef DEBUG  // "#define"ing this will make all TextMeshPro objects with
// TextAnimation cycle through the available animations in AnimationMode

using UnityEngine;
using System;
using System.Collections;
using TMPro;

using Random = UnityEngine.Random;

namespace TMProAnimation
{
    [DisallowMultipleComponent, RequireComponent(typeof(TMP_Text))]
    // TODO: Determine what would make this run smoothly in Edit mode
    // [ExecuteInEditMode]
    public class TextAnimation : MonoBehaviour
    {
        private const int MAX_CHARS_TO_ANIMATE = 32;

        // TODO: Figure out how to keep this enum without spamming the
        // Unity Inspector view with extra labels
        public enum AnimationMode
        {
            None = -1,

            ChangeColor,    // Change colors dynamically (TBD)
            FirstAnimationMode = ChangeColor,
            Wave,           // Wave up and down - like an ocean wave
            FirstMovingAnimationMode = Wave,
            UpAndDown,      // Whole text goes up and down (modified Wave)
            Jitter,         // Jitter and shake rapidly in place
            JitterTogether, // Whole text jitters in concert
            Dangle,         // Sway like a banana from the top-center
            DangleTogether, // Whole text sways in concert
            LastAnimationMode = DangleTogether,

            // Used for cycling through animation modes
            NumActiveAnimationModes = LastAnimationMode - FirstAnimationMode + 1,
            NumAnimationModes = NumActiveAnimationModes + 1
        }

        // Text to animate vars
        private TMP_Text m_textMeshPro;

        [Header("Animation Modifiers")]
        private const float MAX_AM_VAL = 5f;
        private const float MIN_AM_VAL = .1f;
        [Range(MIN_AM_VAL, MAX_AM_VAL)]
        public float AngleMultiplier = 1f;
        [Range(MIN_AM_VAL, MAX_AM_VAL)]
        public float SpeedMultiplier = 1f;
        [Range(MIN_AM_VAL, MAX_AM_VAL)]
        [Tooltip("NOTE: Breaks when !=1f with NotifyTextHasChanged, as the size continues to change per call")]
        public float SizeMultiplier = 1f;  // TODO: Breaks when !=1f with NotifyTextHasChanged, as the size continues to change per call
        [Range(MIN_AM_VAL, MAX_AM_VAL)]
        public float CurveScale = 1f;

        [InspectorName("Animation Mode"), Space(10)]
        [SerializeField] private AnimationMode MeshAnimationMode = AnimationMode.None;
        private bool hasTextChanged;

        // Jitter anim vars
        /// <summary>
        /// Structure to hold pre-computed animation data.
        /// </summary>
        private struct JitterAnim
        {
            public float angleRange;
            public float angle;
            public float speed;
        }
        [Header("Jitter Parameters")]
        private const float MAX_JP_SPEED_RANGE = 10f;
        private const float MIN_JP_SPEED_RANGE = .1f;
        private const float MAX_JP_ANGLE_RANGE = 75f;
        private const float MIN_JP_ANGLE_RANGE = 5f;
        private const float MAX_JP_DIST_RANGE = 15f;
        private const float MIN_JP_DIST_RANGE = .5f;
        private const float MAX_JP_TWIST_RANGE = 20f;
        private const float MIN_JP_TWIST_RANGE = 1f;
        [Range(MIN_JP_SPEED_RANGE, MAX_JP_SPEED_RANGE)]
        public float maxJitterSpeed = 3f;
        [Range(MIN_JP_SPEED_RANGE, MAX_JP_SPEED_RANGE)]
        public float minJitterSpeed = 1f;
        [Range(MIN_JP_ANGLE_RANGE, MAX_JP_ANGLE_RANGE)]
        public float maxJitterAngle = 25f;
        [Range(MIN_JP_ANGLE_RANGE, MAX_JP_ANGLE_RANGE)]
        public float minJitterAngle = 10f;
        [Range(MIN_JP_DIST_RANGE, MAX_JP_DIST_RANGE)]
        [Tooltip("Modifies how far letters move in the jitter animation")]
        public float jitterDistance = .25f;
        [Range(MIN_JP_TWIST_RANGE, MAX_JP_TWIST_RANGE)]
        [Tooltip("Modifies how much letters twist and turn in the jitter animation")]
        public float jitterTwist = 5f;

        // Wave anim vars
        [Header("Wave Parameters")]
        private const float MAX_WAVE_HEIGHT = 10f;
        private const float MIN_WAVE_HEIGHT = .1f;
        private const float MAX_WAVE_SPEED = 10f;
        private const float MIN_WAVE_SPEED = .1f;
        [Range(MIN_WAVE_HEIGHT, MAX_WAVE_HEIGHT)]
        public float maxWaveHeight = 3f;
        [Range(MIN_WAVE_SPEED, MAX_WAVE_SPEED)]
        public float maxWaveSpeed = 3f;

        // Dangle anim vars
        [Header("Dangle Parameters")]
        private const float MAX_DANGLE_SPEED_RANGE = 10f;
        private const float MIN_DANGLE_SPEED_RANGE = .1f;
        private const float MAX_DANGLE_ANGLE_RANGE = 75f;
        private const float MIN_DANGLE_ANGLE_RANGE = 5f;
        [Range(MIN_DANGLE_SPEED_RANGE, MAX_DANGLE_SPEED_RANGE)]
        public float maxDangleSpeed = 3f;
        [Range(MIN_DANGLE_SPEED_RANGE, MAX_DANGLE_SPEED_RANGE)]
        public float minDangleSpeed = 1f;
        [Range(MIN_DANGLE_ANGLE_RANGE, MAX_DANGLE_ANGLE_RANGE)]
        public float maxDangleAngle = 25f;
        [Range(MIN_DANGLE_ANGLE_RANGE, MAX_DANGLE_ANGLE_RANGE)]
        public float minDangleAngle = 10f;

        [ContextMenu("Randomize Animation Modifiers")]
        private void ChooseRandomAMValues()
        {
            AngleMultiplier = Random.Range(MIN_AM_VAL, MAX_AM_VAL);
            SpeedMultiplier = Random.Range(MIN_AM_VAL, MAX_AM_VAL);
            SizeMultiplier = Random.Range(MIN_AM_VAL, MAX_AM_VAL);
            CurveScale = Random.Range(MIN_AM_VAL, MAX_AM_VAL);
        }

#if DEBUG
        private const float TEST_TIME_PER_ANIM = 2f;
        private float debugAnimTestTimer = 0f;
#endif

        void Start()
        {
            Debug.Log("[TextAnimation] Start");
#if !DEBUG
            StartAnimation();
#endif
        }

#if DEBUG
        void FixedUpdate(){
            debugAnimTestTimer += Time.fixedDeltaTime;
            if (debugAnimTestTimer >= TEST_TIME_PER_ANIM)
            {
                int animMode = (int)MeshAnimationMode + 1;
                Debug.Log("AnimMode: " + animMode);
                // If we've gone through all active animations, restart
                if (animMode >= (int)AnimationMode.NumActiveAnimationModes)
                {
                    animMode = (int)AnimationMode.None;
                }
                MeshAnimationMode = (AnimationMode)animMode;
                StartAnimation();
                debugAnimTestTimer = 0f;
            }
        }
#endif

        void Awake()
        {
            m_textMeshPro = gameObject.GetComponent<TMP_Text>();

            // Force vert-mesh to render so we can modify the mesh before
            // our real and final render
            m_textMeshPro.ForceMeshUpdate();
        }

        void StartAnimation()
        {
            StopAllCoroutines();
            ResetMeshVertices();
            switch (MeshAnimationMode)
            {
                case AnimationMode.ChangeColor:
                    Debug.Log("[TextAnimation] case AnimationMode.ChangeColor");
                    StartCoroutine(AnimateVertexColors());
                    break;
                case AnimationMode.Wave:
                    Debug.Log("[TextAnimation] case AnimationMode.Wave");
                    StartCoroutine(AnimateVertexPositionsWave());
                    break;
                case AnimationMode.UpAndDown:
                    Debug.Log("[TextAnimation] case AnimationMode.UpAndDown");
                    StartCoroutine(AnimateVertexPositionsWave());
                    break;
                case AnimationMode.Jitter:
                    Debug.Log("[TextAnimation] case AnimationMode.Jitter");
                    StartCoroutine(AnimateVertexPositionsJitter());
                    break;
                case AnimationMode.JitterTogether:
                    Debug.Log("[TextAnimation] case AnimationMode.JitterTogether");
                    StartCoroutine(AnimateVertexPositionsJitter());
                    break;
                case AnimationMode.Dangle:
                    Debug.Log("[TextAnimation] case AnimationMode.Dangle");
                    StartCoroutine(AnimateVertexPositionsDangle());
                    break;
                case AnimationMode.DangleTogether:
                    Debug.Log("[TextAnimation] case AnimationMode.DangleTogether");
                    StartCoroutine(AnimateVertexPositionsDangle());
                    break;
                case AnimationMode.None:
                default:
                    Debug.Log("[TextAnimation] case None");
                    break;
            }
        }

        public void StopAnimation()
        {
            MeshAnimationMode = AnimationMode.None;
            StopAllCoroutines();
            ResetMeshVertices();
        }

        public AnimationMode SetAnimationMode(AnimationMode animMode)
        {
            if (animMode != MeshAnimationMode)
            {
                MeshAnimationMode = animMode;
                StartAnimation();
            }
            return MeshAnimationMode;
        }

        public AnimationMode GetAnimationMode()
        {
            return MeshAnimationMode;
        }

        [ContextMenu("Reset Text Position and Vertices")]
        void ResetMeshVertices()
        {
            m_textMeshPro.ForceMeshUpdate();
        }

        // Call this whenever something has modified the text content
        public void NotifyTextHasChanged()
        {
            hasTextChanged = true;
        }

        IEnumerator AnimateVertexColors()
        {
            // Force the text object to update right away so we can have geometry to modify right from the start.
            m_textMeshPro.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_textMeshPro.textInfo;
            int currentCharacter = 0;

            Color32[] newVertexColors;
            Color32 c0 = m_textMeshPro.color;

            while (true)
            {
                // If the animation method changed from the one mapped to this
                // function, break
                if (MeshAnimationMode != AnimationMode.ChangeColor)
                {
                    yield break;
                }

                int characterCount = textInfo.characterCount;

                // If No Characters then just yield and wait for some text to be added
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[currentCharacter].materialReferenceIndex;

                // Get the vertex colors of the mesh used by this text element (character or sprite).
                newVertexColors = textInfo.meshInfo[materialIndex].colors32;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[currentCharacter].vertexIndex;

                // Only change the vertex color if the text element is visible.
                if (textInfo.characterInfo[currentCharacter].isVisible)
                {
                    c0 = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);

                    newVertexColors[vertexIndex + 0] = c0;
                    newVertexColors[vertexIndex + 1] = c0;
                    newVertexColors[vertexIndex + 2] = c0;
                    newVertexColors[vertexIndex + 3] = c0;

                    // New function which pushes (all) updated vertex data to the appropriate meshes when using either the Mesh Renderer or CanvasRenderer.
                    m_textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                    // This last process could be done to only update the vertex data that has changed as opposed to all of the vertex data but it would require extra steps and knowing what type of renderer is used.
                    // These extra steps would be a performance optimization but it is unlikely that such optimization will be necessary.
                }

                currentCharacter = (currentCharacter + 1) % characterCount;

                yield return new WaitForSeconds(0.05f);
            }
        }

        IEnumerator AnimateVertexPositionsWave()
        {
            // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
            // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
            m_textMeshPro.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_textMeshPro.textInfo;

            int loopCount = 0;
            hasTextChanged = true;

            // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
            TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            while (true)
            {
                // Get new copy of vertex data if the text has changed.
                if (hasTextChanged)
                {
                    // Update the copy of the vertex data for the text object.
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                    hasTextChanged = false;
                }

                int characterCount = textInfo.characterCount;

                // If No Characters then just yield and wait for some text to be added
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    // Skip characters that are not visible and thus have no geometry to manipulate.
                    if (!charInfo.isVisible)
                        continue;

                    // Retrieve the pre-computed animation data for the given character.
                    // float wAnim = waveAnim[i];

                    // Get the index of the material used by the current character.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the cached vertices of the mesh used by this text element (character or sprite).
                    Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                    Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    // Calculate the height of the current char

                    // UpAndDown doesn't modify the value per char
                    int waveMod = loopCount;
                    if (MeshAnimationMode == AnimationMode.Wave)
                    {
                        waveMod += i;
                    }
                    float wAnim = Mathf.SmoothStep(-maxWaveHeight, maxWaveHeight, Mathf.PingPong(waveMod / 25f * maxWaveSpeed * SpeedMultiplier, 1f));
                    Vector2 waveOffset = new Vector2(0, wAnim);

                    // Need to translate all 4 vertices of each quad according
                    // to the new char height
                    Vector3 offset = waveOffset;

                    destinationVertices[vertexIndex + 0] += offset;
                    destinationVertices[vertexIndex + 1] += offset;
                    destinationVertices[vertexIndex + 2] += offset;
                    destinationVertices[vertexIndex + 3] += offset;
                }

                // Push changes into meshes
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    m_textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                loopCount += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }

        IEnumerator AnimateVertexPositionsJitter()
        {
            // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
            // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
            m_textMeshPro.ForceMeshUpdate();

            TMP_TextInfo textInfo = m_textMeshPro.textInfo;

            Matrix4x4 matrix;

            int loopCount = 0;
            hasTextChanged = true;

            // Create an Array which contains pre-computed Angle Ranges and Speeds for a bunch of characters.
            JitterAnim[] vertexAnim = new JitterAnim[MAX_CHARS_TO_ANIMATE];

            if (MeshAnimationMode == AnimationMode.Jitter)
            {
                for (int i = 0; i < MAX_CHARS_TO_ANIMATE; i++)
                {
                    vertexAnim[i].angleRange = Random.Range(minJitterAngle, maxJitterAngle);
                    vertexAnim[i].speed = Random.Range(minJitterSpeed, maxJitterSpeed);
                }
            }
            else
            {
                float angleRange = Random.Range(minJitterAngle, maxJitterAngle);
                float speed = Random.Range(minJitterSpeed, maxJitterSpeed);
                for (int i = 0; i < MAX_CHARS_TO_ANIMATE; i++)
                {
                    vertexAnim[i].angleRange = angleRange;
                    vertexAnim[i].speed = speed;
                }
            }

            // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
            TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            while (true)
            {
                // Get new copy of vertex data if the text has changed.
                if (hasTextChanged)
                {
                    // Update the copy of the vertex data for the text object.
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

                    hasTextChanged = false;
                }

                int characterCount = textInfo.characterCount;

                // If No Characters then just yield and wait for some text to be added
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                // Universal jitter vals for JitterTogether
                float randJitter1 = Random.Range(-jitterDistance, jitterDistance);
                float randJitter2 = Random.Range(-jitterDistance, jitterDistance);
                float randJitter3 = Random.Range(-jitterTwist, jitterTwist);

                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    // Skip characters that are not visible and thus have no geometry to manipulate.
                    if (!charInfo.isVisible)
                        continue;

                    // Retrieve the pre-computed animation data for the given character.
                    JitterAnim vertAnim = vertexAnim[i];

                    // Get the index of the material used by the current character.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the cached vertices of the mesh used by this text element (character or sprite).
                    Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                    // Determine the center point of each character at the baseline.
                    //Vector2 charMidBasline = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                    // Determine the center point of each character.
                    Vector2 charMidBasline = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;

                    // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                    // This is needed so the matrix TRS is applied at the origin for each character.
                    Vector3 offset = charMidBasline;

                    Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                    destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                    destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                    destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                    vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(loopCount /jitterDistance * vertAnim.speed * SpeedMultiplier, 1f));

                    if (MeshAnimationMode == AnimationMode.Jitter)
                    {
                        randJitter1 = Random.Range(-jitterDistance, jitterDistance);
                        randJitter2 = Random.Range(-jitterDistance, jitterDistance);
                        randJitter3 = Random.Range(- jitterTwist, jitterTwist);
                    }
                    Vector3 jitterOffset = new Vector3(randJitter1, randJitter2, 0);

                    matrix = Matrix4x4.TRS(jitterOffset * CurveScale, Quaternion.Euler(0, 0, randJitter3 * AngleMultiplier), Vector3.one * SizeMultiplier);

                    destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                    destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                    destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                    destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                    destinationVertices[vertexIndex + 0] += offset;
                    destinationVertices[vertexIndex + 1] += offset;
                    destinationVertices[vertexIndex + 2] += offset;
                    destinationVertices[vertexIndex + 3] += offset;

                    vertexAnim[i] = vertAnim;
                }

                // Push changes into meshes
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    m_textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                loopCount += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }

        IEnumerator AnimateVertexPositionsDangle()
        {
            // Same logic as Jitter, but it pivots from the top-center instead
            // of the center of the char glyph

            // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
            // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
            m_textMeshPro.ForceMeshUpdate();
            Vector3[] vertices = m_textMeshPro.mesh.vertices;

            TMP_TextInfo textInfo = m_textMeshPro.textInfo;

            Matrix4x4 matrix;

            int loopCount = 0;
            hasTextChanged = true;

            // Create an Array which contains pre-computed Angle Ranges and Speeds for a bunch of characters.
            JitterAnim[] vertexAnim = new JitterAnim[MAX_CHARS_TO_ANIMATE];
            if (MeshAnimationMode == AnimationMode.Dangle)
            {
                for (int i = 0; i < MAX_CHARS_TO_ANIMATE; i++)
                {
                    vertexAnim[i].angleRange = Random.Range(minDangleAngle, maxDangleAngle);
                    vertexAnim[i].speed = Random.Range(minDangleSpeed, maxDangleSpeed);
                }
            }

            // Cache the vertex data of the text object as the Jitter FX is applied to the original position of the characters.
            TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

            while (true)
            {
                // Get new copy of vertex data if the text has changed.
                if (hasTextChanged)
                {
                    // Update the copy of the vertex data for the text object.
                    cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
                    vertices = m_textMeshPro.mesh.vertices;

                    hasTextChanged = false;
                }

                int characterCount = textInfo.characterCount;

                // If No Characters then just yield and wait for some text to be added
                if (characterCount == 0)
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                for (int i = 0; i < characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                    // Skip characters that are not visible and thus have no geometry to manipulate.
                    if (!charInfo.isVisible)
                        continue;

                    // Get the index of the material used by the current character.
                    int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                    // Get the index of the first vertex used by this text element.
                    int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                    // Get the cached vertices of the mesh used by this text element (character or sprite).
                    Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                    // Determine the top-center point of each character.
                    Vector2 charTopCenter = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.topRight.y);

                    // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                    // This is needed so the matrix TRS is applied at the origin for each character.
                    Vector3 offset = charTopCenter;

                    Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                    destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                    destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                    destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                    destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                    // UpAndDown doesn't modify the value per char
                    int dangleMod = loopCount;
                    float dangleAngle;
                    JitterAnim vertAnim = vertexAnim[i];
                    if (MeshAnimationMode == AnimationMode.Dangle)
                    {
                        dangleMod += i;

                        // Retrieve the pre-computed animation data for the given character.

                        vertAnim.angle = Mathf.SmoothStep(-vertAnim.angleRange, vertAnim.angleRange, Mathf.PingPong(dangleMod / 25f * vertAnim.speed * SpeedMultiplier, 1f));

                        dangleAngle = vertAnim.angle;
                    }
                    else
                    {
                        dangleAngle = Mathf.SmoothStep(-maxDangleAngle, maxDangleAngle, Mathf.PingPong(dangleMod / 25f * maxDangleSpeed * SpeedMultiplier, 1f));
                    }

                    matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, dangleAngle * AngleMultiplier), Vector3.one * SizeMultiplier);

                    destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                    destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                    destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                    destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                    destinationVertices[vertexIndex + 0] += offset;
                    destinationVertices[vertexIndex + 1] += offset;
                    destinationVertices[vertexIndex + 2] += offset;
                    destinationVertices[vertexIndex + 3] += offset;

                    if (MeshAnimationMode == AnimationMode.Dangle)
                    {
                        vertexAnim[i] = vertAnim;
                    }
                }

                // Push changes into meshes
                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                    m_textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
                }

                loopCount += 1;

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}