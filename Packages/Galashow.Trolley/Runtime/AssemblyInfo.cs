using UnityEngine.Scripting;

// 다른 어셈블리가 직접 참조하지 않아도 WebGL 빌드의 코드 스트리핑에서 제외되지 않게 한다.
// 플러그인은 TrolleyPluginInstaller가 로드 시점에 GamePluginCatalog에 등록한다.
[assembly: AlwaysLinkAssembly]
