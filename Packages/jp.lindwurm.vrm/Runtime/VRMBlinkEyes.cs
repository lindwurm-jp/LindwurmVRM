using UnityEngine;
using UniVRM10;

namespace Lindwurm.VRM
{
    public class VRMBlinkEyes : MonoBehaviour
    {
        public float BlinkTime = 0.05f;
        private Vrm10RuntimeExpression runtimeExpression;
        int blink;
        float wait;
        float blinkPlay;

        private void Awake()
        {
            if (TryGetComponent<Vrm10Instance>(out var instance))
                runtimeExpression = instance.Runtime.Expression;
            blink = 0;
            wait = 3f;
        }

        // Update is called once per frame
        void Update()
        {
            if (blink == 0)
            {
                wait -= Time.deltaTime;
                if (wait <= 0f)
                {
                    blink = 1;
                    blinkPlay = 0f;
                    wait = Random.Range(1.0f, 5.0f);
                }
            }
            else
            {
                blinkPlay += Time.deltaTime;
                float weight;
                if (blink == 1)
                {
                    if (blinkPlay <= BlinkTime)
                    {
                        weight = blinkPlay / BlinkTime;
                    }
                    else
                    {
                        weight = 1f;
                        blink++;
                        blinkPlay -= BlinkTime;
                    }
                }
                else
                {
                    if (blinkPlay <= BlinkTime)
                    {
                        weight = 1 - blinkPlay / BlinkTime;
                    }
                    else
                    {
                        weight = 0f;
                        blink = 0;
                    }
                }
                runtimeExpression.SetWeight(ExpressionKey.Blink, weight);
            }
        }
    }
}
