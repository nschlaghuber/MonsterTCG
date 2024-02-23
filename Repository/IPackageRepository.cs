using MonsterTCG.Model.Package;

namespace MonsterTCG.Repository;

public interface IPackageRepository
{
    public Task<Package?> FindPackageByIdAsync(string id);
    public Task<Package?> FindPackageByCardIdsAsync((string?, string?, string?, string?, string?) cardIds);
    public Task<Package?> FindFirstPackageAsync();
    public Task<bool> ExistsByCardIdsAsync((string?, string?, string?, string?, string?) cardIds);
    public Task CreatePackageAsync(Package package);
    public Task<bool> DeletePackageByIdAsync(string packageId);
}