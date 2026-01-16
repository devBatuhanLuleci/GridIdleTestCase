namespace BoardGameTestCase.Core.Common
{
    public interface IInitializable
    {
        void Initialize();
        bool IsInitialized { get; }
    }
}

