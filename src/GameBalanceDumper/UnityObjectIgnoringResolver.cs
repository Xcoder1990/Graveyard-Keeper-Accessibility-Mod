namespace GameBalanceDumper;

// Skips members whose declared type derives from UnityEngine.Object (Sprite, Texture2D,
// GameObject, MonoBehaviour, WorldGameObject, AudioClip, ...) and members marked
// [NonSerialized]. Both are unsafe-to-serialize for very different reasons:
//   - Unity Object refs would either crash the serializer, follow into native engine
//     state we can't read, or pull in megabytes of unrelated scene data.
//   - [NonSerialized] is the game's own signal that a field is runtime-only
//     (e.g., SmartExpression._wgo / _character bind back to live WorldGameObjects).
internal sealed class UnityObjectIgnoringResolver : DefaultContractResolver
{
    public override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);

        var declared = (member as FieldInfo)?.FieldType
                    ?? (member as PropertyInfo)?.PropertyType;

        if (declared != null && typeof(UnityEngine.Object).IsAssignableFrom(declared))
        {
            prop.Ignored = true;
            prop.ShouldSerialize = _ => false;
        }

        if (member is FieldInfo fi && fi.IsNotSerialized)
        {
            prop.Ignored = true;
            prop.ShouldSerialize = _ => false;
        }

        return prop;
    }
}
