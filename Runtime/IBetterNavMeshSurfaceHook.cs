using Unity.AI.Navigation;

namespace Lajawi
{
    public interface IBetterNavMeshSurfaceHook
    {
        void OnPreBake(NavMeshSurface surface);
        void OnPostBake(NavMeshSurface surface);
    }
}
