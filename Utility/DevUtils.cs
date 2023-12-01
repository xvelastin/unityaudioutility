using UnityEngine;

namespace XV
{
    public class DevUtils
    {
        public static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            return obj.TryGetComponent(out T component) ? component : obj.AddComponent<T>();
        }
        
        public static float MapToRange(float value, float originalStart, float originalEnd, float newStart, float newEnd)
        {
            // credit to Wim Coenen //
            var scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (float)(newStart + ((value - originalStart) * scale));
        }
        
        
        public static float ApplyRandom(float centreValue, float modulationValue)
        {
            return centreValue + Random.Range(-modulationValue / 2, modulationValue / 2);
        }

        public static int ApplyRandom(int centreValue, int modulationValue)
        {
            return Mathf.RoundToInt(centreValue +
                                    Random.Range(-(float)modulationValue / 2, (float)modulationValue / 2));
        }

        public static Vector3 ApplyRandom(Vector3 centreValue, Vector3 modulationValue)
        {
            var x = centreValue.x + Random.Range(-modulationValue.x / 2, modulationValue.x / 2);
            var y = centreValue.y + Random.Range(-modulationValue.y / 2, modulationValue.y / 2);
            var z = centreValue.z + Random.Range(-modulationValue.z / 2, modulationValue.z / 2);
            return new Vector3(x, y, z);
        }

        public static Vector3 ApplyRandom(Vector3 centreValue, float modulationValue)
        {
            var x = centreValue.x + Random.Range(-modulationValue / 2, modulationValue / 2);
            var y = centreValue.y + Random.Range(-modulationValue / 2, modulationValue / 2);
            var z = centreValue.z + Random.Range(-modulationValue / 2, modulationValue / 2);
            return new Vector3(x, y, z);
        }

        public static void Shuffle<T>(T[] array)
        {
            var len = array.Length;
            while (len > 1)
            {
                len--;
                var k = Random.Range(0, len + 1);
                (array[k], array[len]) = (array[len], array[k]);
            }
        }

    }

    public static class AudioDebug
    {
        private const string audioPrefix = "Audio";
        
        public static void Log(string message) => Debug.Log($"<color=white>{audioPrefix}</color> ~ {message}");
        public static void LogMethod(string append = "")
        {
            Debug.Log($"<color=white>{audioPrefix}</color> ~ {new System.Diagnostics.StackFrame(1).GetMethod().Name} invoked. {append}");
        }
    }
}