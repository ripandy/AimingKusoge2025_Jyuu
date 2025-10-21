using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IBeePresenterFactory
    {
        UniTask<(IBeePresenter, IBeeMoveController, IBeeHarvestPresenter, IBeeStoreNectarPresenter, IBeeAudioPresenter)> Create(int beeId, CancellationToken cancellationToken = default);
        void Clear();
    }
}