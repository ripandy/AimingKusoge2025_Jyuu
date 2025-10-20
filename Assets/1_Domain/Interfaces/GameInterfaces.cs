using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IBeePresenterFactory
    {
        UniTask<(IBeePresenter, IBeeMoveController, IBeeHarvestPresenter, IBeeStoreNectarPresenter)> Create(int beeId, CancellationToken cancellationToken = default);
        void Clear();
    }
}