namespace EscapeThe90s.Contracts
{
    /// <summary>Implemented by a component that accepts a required service through composition.</summary>
    public interface IDependent<T>
    {
        void Inject(T dependency);
    }
}
