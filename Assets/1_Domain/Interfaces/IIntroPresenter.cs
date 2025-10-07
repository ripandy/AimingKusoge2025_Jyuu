using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IIntroPresenter
    {
        UniTask ShowAsync(CancellationToken cancellationToken = default);
    }
}