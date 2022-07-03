#include <vector>
#include <cstdint>

#include "pluginsdk/bridgemain.h"
#include "pluginsdk/_plugins.h"
#include "pluginsdk/_scriptapi_memory.h"

namespace Dotx64Dbg::Native
{
    using namespace System;
    using namespace System::Runtime::InteropServices;

    [UnmanagedFunctionPointer(CallingConvention::Cdecl)]
    public delegate void PluginCallback(int argc, System::IntPtr);

    public ref class Callbacks
    {
    public:
        static void RegisterCallback(int pluginHandle, int cbType, PluginCallback^ cb)
        {
            GCHandle gcCb = GCHandle::Alloc(cb);

            IntPtr ip = Marshal::GetFunctionPointerForDelegate(cb);
            auto* fn = static_cast<CBPLUGIN>(ip.ToPointer());

            _plugin_registercallback(pluginHandle, static_cast<CBTYPE>(cbType), fn);
        }

        static bool UnregisterCallback(int pluginHandle, int cbType)
        {
            return _plugin_unregistercallback(pluginHandle, static_cast<CBTYPE>(cbType));
        }
    };
}
