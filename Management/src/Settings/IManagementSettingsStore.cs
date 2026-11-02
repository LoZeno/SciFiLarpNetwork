namespace SciFiLarpNetwork.Management.Settings;

public interface IManagementSettingsStore
{
    ManagementSettings Load();

    void Save(ManagementSettings settings);
}
