namespace RulesEngine.Core
{
    public class Artifact
    {
        public string? Id { get; }
        public object Value { get; }
        public Artifact(object value, string? id = null)
        {
            Value = value;
            Id = id;
        }
    }
}