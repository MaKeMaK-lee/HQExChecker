namespace HQExChecker.GUI.Extensions
{
    public static class ObjectExtensions
    {
        public static bool EqualProps(this object left, object right)
        {
            if (left.GetType() != right.GetType())
                return false;

            var propInfos = left.GetType().GetProperties();

            foreach (var propInfo in propInfos)
            {
                dynamic leftV = propInfo.GetValue(left)!;
                dynamic rightV = propInfo.GetValue(right)!;
                if (leftV != rightV)
                    return false;
            }

            return true;
        }
    }
}
