namespace IngameScript
{
    static class MyUtil
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        public static void Sort<T>(ref float ka, ref float kb, ref T a, ref T b)
        {//sort to ka >= bc
            if (ka < kb)
            {
                Swap(ref ka, ref kb);
                Swap(ref a, ref b);
            }
        }
        public static void Sort<T>(ref float ka, ref float kb, ref float kc, ref T a, ref T b, ref T c)
        {//sort ka >= kb >= kc
            Sort(ref ka, ref kb, ref a, ref b);
            Sort(ref kb, ref kc, ref b, ref c);
            Sort(ref ka, ref kb, ref a, ref b);
        }
        public static void Sort<T>(float ka, float kb, float kc, ref T a, ref T b, ref T c)
        {
            Sort(ref ka, ref kb, ref a, ref b);
            Sort(ref kb, ref kc, ref b, ref c);
            Sort(ref ka, ref kb, ref a, ref b);
        }
        public static void SortSymmetricPairs<T>(ref float kab, ref float kbc, ref float kac, ref T a, ref T b, ref T c)
        {
            if (kab < kbc)
            {
                Swap(ref kab, ref kbc);
                Swap(ref a, ref c);
            }
            if (kab < kac)
            {
                Swap(ref kab, ref kac);
                Swap(ref b, ref c);
            }
            if (kbc > kac)
            {
                Swap(ref kbc, ref kac);
                Swap(ref a, ref b);
            }
        }

        public static bool TryCast<T>(this object obj, out T result)
        {
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }

            result = default(T);
            return false;
        }

    }
}