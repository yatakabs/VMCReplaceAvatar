using UnityEngine;

namespace VMCReplaceAvatar
{
    public class BlendShapeSync : MonoBehaviour
    {
        public Renderer sourceRenderer;

        private void Update()
        {
            if (sourceRenderer == null) return;
            SkinnedMeshRenderer sourceSkinnedMeshRenderer = sourceRenderer as SkinnedMeshRenderer;
            SkinnedMeshRenderer targetSkinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if (sourceSkinnedMeshRenderer == null || targetSkinnedMeshRenderer == null) return;
            int blendShapeCount = sourceSkinnedMeshRenderer.sharedMesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                string blendShapeName = sourceSkinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
                int targetBlendShapeIndex = targetSkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendShapeName);
                if (targetBlendShapeIndex != -1)
                {
                    float weight = sourceSkinnedMeshRenderer.GetBlendShapeWeight(i);
                    targetSkinnedMeshRenderer.SetBlendShapeWeight(targetBlendShapeIndex, weight);
                }
            }
        }
    }
}
