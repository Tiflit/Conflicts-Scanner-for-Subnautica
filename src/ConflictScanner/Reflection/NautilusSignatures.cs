using System.Reflection;

namespace ConflictScanner.Reflection
{
    public static class NautilusSignatures
    {
        public static bool IsTechTypeRegistration(MethodInfo method)
        {
            return method.Name == "AddTechType" &&
                   method.DeclaringType?.Name == "TechTypeHandler";
        }

        public static bool IsCraftTreeRegistration(MethodInfo method)
        {
            return method.Name == "AddNode" &&
                   method.DeclaringType?.Name == "CraftTreeHandler";
        }

        public static bool IsSpriteRegistration(MethodInfo method)
        {
            return method.Name == "RegisterSprite" &&
                   method.DeclaringType?.Name == "SpriteHandler";
        }
    }
}
