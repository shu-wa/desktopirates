using System.Collections;
using UnityEngine;

namespace Desktopirates
{
    /// <summary>
    /// Streams the four distant boss asset sets into the Resources cache without forcing
    /// mesh/texture GPU uploads on the sailing thread. The old synchronous native warm-up
    /// produced an intermittent half-second frame while the desktop widget was already live.
    /// </summary>
    public sealed class BossAssetWarmupController : MonoBehaviour
    {
        private readonly Object[] cachedAssets = new Object[8];

        private IEnumerator Start()
        {
            for (int value = (int)BossKind.GangAdmiral; value <= (int)BossKind.Poseidon; value++)
            {
                BossKind boss = (BossKind)value;
                int index = (value - (int)BossKind.GangAdmiral) * 2;

                ResourceRequest modelRequest = Resources.LoadAsync<GameObject>(AuthoredModelLibrary.GetBossResourcePath(boss));
                yield return modelRequest;
                cachedAssets[index] = modelRequest.asset;

                ResourceRequest textureRequest = Resources.LoadAsync<Texture2D>(AuthoredModelLibrary.GetBossTexturePath(boss));
                yield return textureRequest;
                cachedAssets[index + 1] = textureRequest.asset;
                yield return null;
            }
            enabled = false;
        }
    }
}
