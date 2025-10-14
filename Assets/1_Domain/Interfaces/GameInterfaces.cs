using System.Threading;
using Cysharp.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IBeePresenterFactory
    {
        UniTask<IBeeMoveController> CreateBeeMoveController(Bee bee, CancellationToken cancellationToken = default);
        UniTask<IBeeHarvestPresenter> CreateBeeHarvestPresenter(Bee bee, CancellationToken cancellationToken = default);
        UniTask<IBeeStorePollenPresenter> CreateBeeStorePollenPresenter(Bee bee, CancellationToken cancellationToken = default);
    }
}