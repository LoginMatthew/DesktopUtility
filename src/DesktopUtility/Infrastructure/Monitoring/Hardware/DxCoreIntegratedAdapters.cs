using System.Runtime.InteropServices;

namespace DesktopUtility.Infrastructure.Monitoring;

internal static class DxCoreIntegratedAdapters
{
    private static readonly Guid GraphicsAdapterAttribute = new("0c9ece4d-2f6e-4f01-8c96-e89e331b47b1");

    public static IReadOnlyList<IntegratedAdapter> Read()
    {
        var factoryId = typeof(IAdapterFactory).GUID;

        if (DXCoreCreateAdapterFactory(in factoryId, out var factory) < 0)
        {

            return [];
        }

        try
        {
            var listId = typeof(IAdapterList).GUID;

            if (factory.CreateAdapterList(1, in GraphicsAdapterAttribute, in listId, out var adapters) < 0)
            {

                return [];
            }

            try
            {
                var found = new List<IntegratedAdapter>();
                var adapterId = typeof(IAdapter).GUID;
                uint count = adapters.GetAdapterCount();

                for (uint index = 0; index < count; index++)
                {

                    if (adapters.GetAdapter(index, in adapterId, out var adapter) >= 0)
                    {
                        try
                        {

                            if (ReadIntegratedAdapter(adapter) is { } integrated)
                            {
                                found.Add(integrated);
                            }
                        }
                        finally
                        {
                            Marshal.ReleaseComObject(adapter);
                        }
                    }
                }

                return found;
            }
            finally
            {
                Marshal.ReleaseComObject(adapters);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(factory);
        }
    }

    private static IntegratedAdapter? ReadIntegratedAdapter(IAdapter adapter)
    {
        const int maximumDescriptionLength = 4096;
        nint buffer = Marshal.AllocHGlobal(maximumDescriptionLength);

        try
        {

            if (adapter.GetProperty(AdapterProperty.IsHardware, 1, buffer) < 0 || Marshal.ReadByte(buffer) == 0
                || adapter.GetProperty(AdapterProperty.IsIntegrated, 1, buffer) < 0 || Marshal.ReadByte(buffer) == 0
                || adapter.GetProperty(AdapterProperty.InstanceLuid, 8, buffer) < 0)
            {

                return null;
            }

            uint low = unchecked((uint)Marshal.ReadInt32(buffer));
            uint high = unchecked((uint)Marshal.ReadInt32(buffer, 4));
            string? name = null;

            if (adapter.GetPropertySize(AdapterProperty.DriverDescription, out nuint length) >= 0
                && length is > 0 and <= maximumDescriptionLength
                && adapter.GetProperty(AdapterProperty.DriverDescription, length, buffer) >= 0)
            {
                name = Marshal.PtrToStringUTF8(buffer, (int)length)?.TrimEnd('\0');
            }

            return new IntegratedAdapter($"luid_0x{high:x8}_0x{low:x8}", name ?? "");
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private enum AdapterProperty { InstanceLuid = 0, DriverDescription = 2, IsHardware = 11, IsIntegrated = 12 }

    [DllImport("dxcore.dll", ExactSpelling = true)]
    private static extern int DXCoreCreateAdapterFactory(in Guid id, [MarshalAs(UnmanagedType.Interface)] out IAdapterFactory factory);

    [ComImport, Guid("78ee5945-c36e-4b13-a669-005dd11c0f06"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAdapterFactory
    {
        [PreserveSig]
        int CreateAdapterList(uint count, in Guid attributes, in Guid id, [MarshalAs(UnmanagedType.Interface)] out IAdapterList list);
    }

    [ComImport, Guid("526c7776-40e9-459b-b711-f32ad76dfc28"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAdapterList
    {
        [PreserveSig]
        int GetAdapter(uint index, in Guid id, [MarshalAs(UnmanagedType.Interface)] out IAdapter adapter);

        [PreserveSig]
        uint GetAdapterCount();
    }

    [ComImport, Guid("f0db4c7f-fe5a-42a2-bd62-f2a6cf6fc83e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAdapter
    {
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I1)]
        bool IsValid();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I1)]
        bool IsAttributeSupported(in Guid attribute);

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I1)]
        bool IsPropertySupported(AdapterProperty property);

        [PreserveSig]
        int GetProperty(AdapterProperty property, nuint size, nint buffer);

        [PreserveSig]
        int GetPropertySize(AdapterProperty property, out nuint size);
    }
}
