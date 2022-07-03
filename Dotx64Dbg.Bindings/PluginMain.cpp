#include "pluginsdk/bridgemain.h"
#include "pluginsdk/_plugins.h"

#include "Marshal.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;

namespace Dotx64Dbg
{
    public value struct SetupStruct
    {
        IntPtr hwndDlg; //gui window handle
        Int32 hMenu; //plugin menu handle
        Int32 hMenuDisasm; //plugin disasm menu handle
        Int32 hMenuDump; //plugin dump menu handle
        Int32 hMenuStack; //plugin stack menu handle
        Int32 hMenuGraph; //plugin graph menu handle
        Int32 hMenuMemmap; //plugin memory map menu handle
        Int32 hMenuSymmod; //plugin symbol module menu handle
    };

    public interface class IPlugin 
    {
        property int Version 
        {
            Int32 get();
        }

        property String^ Name
        {
            String^ get();
        }

        bool Init(int pluginHandle);
        bool Stop();
        void Setup([In] SetupStruct% setupStruct);
    };

    private ref struct Helper
    {
        static IPlugin^ plugin;

        void OnUnhandledException(Object^ sender, UnhandledExceptionEventArgs^ e)
        {
            String^ location = IPlugin::typeid->Assembly->Location;
            String^ logPath = Path::ChangeExtension(location, ".log");

            File::AppendAllText(logPath, e->ExceptionObject->ToString());
        }

        Assembly^ OnAssemblyResolve(Object^ sender, ResolveEventArgs^ args)
        {
            auto assemblyName = gcnew AssemblyName(args->Name);

            if (assemblyName->Name == IPlugin::typeid->Assembly->GetName()->Name)
                return IPlugin::typeid->Assembly;

            auto location = IPlugin::typeid->Assembly->Location;
            auto pluginBasePath = Path::GetDirectoryName(location);
            auto dllPath = Path::Combine(pluginBasePath, assemblyName->Name + ".dll");

            return Assembly::LoadFile(dllPath);
        }
    };
}

#define PLUG_EXPORT extern "C" __declspec(dllexport)


PLUG_EXPORT bool pluginit(PLUG_INITSTRUCT* initStruct)
{
    auto eventHelper = gcnew Dotx64Dbg::Helper();
    AppDomain::CurrentDomain->UnhandledException += gcnew UnhandledExceptionEventHandler(eventHelper, &Dotx64Dbg::Helper::OnUnhandledException);
    AppDomain::CurrentDomain->AssemblyResolve += gcnew ResolveEventHandler(eventHelper, &Dotx64Dbg::Helper::OnAssemblyResolve);

    Type^ pluginType = Type::GetType("DotNetPlugin.Plugin, " + Dotx64Dbg::IPlugin::typeid->Assembly->GetName()->Name + "Impl", true);
    auto plugin = safe_cast<Dotx64Dbg::IPlugin^>(Activator::CreateInstance(pluginType));
    
    initStruct->sdkVersion = 1;
    initStruct->pluginVersion = plugin->Version;

    msclr::interop::marshal_context oMarshalContext;
    const char* pluginName = oMarshalContext.marshal_as<const char*>(plugin->Name);
    strncpy_s(initStruct->pluginName, pluginName, _TRUNCATE);

    if (!plugin->Init(initStruct->pluginHandle))
        return false;

    Dotx64Dbg::Helper::plugin = plugin;
    return true;
}

PLUG_EXPORT bool plugstop()
{
    auto plugin = Dotx64Dbg::Helper::plugin;
    if (!plugin)
        return false;

    plugin->Stop();

    return true;
}

PLUG_EXPORT void plugsetup(PLUG_SETUPSTRUCT* setupStruct)
{
    auto plugin = Dotx64Dbg::Helper::plugin;
    if (!plugin)
        return;

    plugin->Setup(reinterpret_cast<Dotx64Dbg::SetupStruct&>(setupStruct));
}