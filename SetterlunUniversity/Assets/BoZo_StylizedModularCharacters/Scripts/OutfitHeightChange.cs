using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Bozo.ModularCharacters
{
    public class OutfitHeightChange : MonoBehaviour
    {
        [SerializeField] float HeightOffset;
        [Header("Heel Options")]
        [SerializeField] bool heelEnabled;
        [SerializeField] string animParameter = "HeelHeight";
        [SerializeField] string blendName = "AnimShape_HeelHeight";
        [Range(0,1)][SerializeField] float heelHeight;
        [SerializeField] float heelHeightOffset;

        private void Start()
        {
            if (!heelEnabled)
            {
                heelHeightOffset = 0;
                return;
            }

            var System = GetComponentInParent<OutfitSystem>();
            if (System == null) return;
            var animator = System.animator;
            var outfit = GetComponent<Outfit>();

                    if (outfit == null) return;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == animParameter)
                {

                    animator.SetFloat(param.name, heelHeight);
                }
            }

            if (outfit.skinnedRenderer == null) return;
            var mesh = outfit.skinnedRenderer.sharedMesh;
            var index = mesh.GetBlendShapeIndex(blendName);
            if (index == -1) return;

            //removing nameshape that maya gives
            var sort = blendName.Split(".");
            if (sort.Length > 1) { blendName = sort[1]; }

            outfit.skinnedRenderer.SetBlendShapeWeight(index, heelHeight * 100);



        }

        private void OnValidate()
        {
            if(heelEnabled && Application.isPlaying && gameObject.scene.isLoaded)
            {
                var System = GetComponentInParent<OutfitSystem>();
                if (System == null) return;

                var animator = System.animator;

                var outfit = GetComponent<Outfit>();
                if (outfit == null) return;
                if (outfit.skinnedRenderer == null) return;

                var mesh = outfit.skinnedRenderer.sharedMesh;
                var index = mesh.GetBlendShapeIndex(blendName);
                print(index);
                if (index == -1) return;

                var sort = blendName.Split(".");
                if (sort.Length > 1) { blendName = sort[1]; }

                outfit.skinnedRenderer.SetBlendShapeWeight(index, heelHeight * 100);

                var heeledHeight = Mathf.Lerp(0, heelHeightOffset, heelHeight);
                var height = HeightOffset + heeledHeight;

                System.SetHeight(height);
                animator.SetFloat(animParameter, heelHeight);
            }
        }

        private void OnEnable()
        {
            if (!heelEnabled)
            {
                heelHeightOffset = 0;
                heelHeight = 0;
            }

            Invoke("SetHeight", 0);
        }

        private void OnDisable()
        {
            RemoveHeight();
        }

        private void SetHeight()
        {
            var System = GetComponentInParent<OutfitSystem>();
            if (System == null) return;

            var height = HeightOffset + heelHeightOffset;

            System.SetHeight(height);
            System.animator.SetFloat(animParameter, heelHeight);
        }

        private void RemoveHeight()
        {
            var System = GetComponentInParent<OutfitSystem>();
            if (System == null) return;

            System.SetHeight(0);
            System.animator.SetFloat(animParameter, heelHeight);
        }
    }
}
