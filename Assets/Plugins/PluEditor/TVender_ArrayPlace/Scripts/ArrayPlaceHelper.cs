#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace TVender.ArrayPlace
{
    //[HelpURL("https://www.paopaocha.top/tvender/arrayplace")]

    public class ArrayPlaceHelper : MonoBehaviour
    {
        [Tooltip("prefab to be copy")]
        public GameObject Prefab;

        public bool IsUIArray = false;

        [Header("Array Settings")]
        public int HorizontalQuantity = 5;

        public int VerticalQuantity = 5;

        public float HorizontalSpacing = 1f;

        public bool InverseHorizontal = false;

        public float VerticalSpacing = 1f;

        public bool InverseVertical = false;

        public ArrayOffset ArrayOffset;

        [Header("Random")]
        public RandomRange RandomOffset;

        public RandomRange RandomRotation;

        public Vector3 FixedRotation = Vector3.zero;

        public RandomRange RandomScale;

        [Header("Exclusion")]
        public ExclusionRect Exclusion;

        public void Place()
        {
            Clear();

            for (var i = 0; i < VerticalQuantity; i++)
            {
                for (var j = 0; j < HorizontalQuantity; j++)
                {
                    var ox = 0f;
                    var oz = 0f;

                    if (RandomOffset.Active)
                    {
                        //TODO:pro edition has 3 axis random offset
                        ox = UnityEngine.Random.Range(RandomOffset.Min, RandomOffset.Max) + ArrayOffset.Horizontal;
                        oz = UnityEngine.Random.Range(RandomOffset.Min, RandomOffset.Max) + ArrayOffset.Vertical;
                    }
                    else
                    {
                        ox = ArrayOffset.Horizontal;
                        oz = ArrayOffset.Vertical;
                    }

                    var z = i * VerticalSpacing + oz;
                    var x = j * HorizontalSpacing + ox;

                    if (Exclusion.Active && Exclusion.Rect.Contains(new Vector2(x, z)))
                    {
                        continue;
                    }

                    var obj = PrefabUtility.InstantiatePrefab(Prefab) as GameObject;

                    obj.transform.SetParent(transform);

                    if (RandomRotation.Active)
                        obj.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(RandomRotation.Min, RandomRotation.Max), 0);
                    else
                        obj.transform.localRotation = Quaternion.Euler(FixedRotation);

                    if (RandomScale.Active)
                    {
                        float scale = 1f + UnityEngine.Random.Range(RandomScale.Min, RandomScale.Max);
                        obj.transform.localScale = new Vector3(scale, scale, scale);
                    }

                    if (InverseHorizontal)
                        x = -x;
                    if (InverseVertical)
                        z = -z;

                    if (!IsUIArray)
                        obj.transform.localPosition = new Vector3(x, 0, z);
                    else
                    {
                        obj.GetComponent<RectTransform>().anchoredPosition = new Vector3(x, z, 0);
                    }
                }
            }
        }

        public void Clear()
        {
            var childCount = transform.childCount;

            for (int i = 0; i < childCount; i++)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
        }
    }

    [Serializable]
    public class RandomRange
    {
        public bool Active = false;

        public float Min = -0.1f;

        public float Max = 0.1f;
    }

    [Serializable]
    public class ExclusionRect
    {
        public bool Active = false;

        public Rect Rect;
    }

    [Serializable]
    public class ArrayOffset
    {
        public float Horizontal = 0f;

        public float Vertical = 0f;
    }
}
#else
using System;
using UnityEditor;
using UnityEngine;

namespace TVender.ArrayPlace
{
    public class ArrayPlaceHelper : MonoBehaviour
    {
       
    }
}
#endif