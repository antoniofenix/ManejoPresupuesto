namespace ManejoPresupuesto.Helpers
{
    public static class Extensores
    {
        public static bool IsNull(this object objeto)
        {
            try
            {
                if(objeto is null || string.IsNullOrEmpty(objeto.ToString()))
                {
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public static bool NotIsNull(this object objeto)
        {
            try
            {
                if (objeto is null || string.IsNullOrEmpty(objeto.ToString()))
                {
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public static int ToInt32(this object objeto)
        {
            try
            {
                return Convert.ToInt32(objeto.ToString());
            }
            catch { return 0; }
        }
    }
}
