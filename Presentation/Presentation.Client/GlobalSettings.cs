namespace Presentation.Client;

public static class GlobalSettings
{
    /*
     * This is used when we are hosting the application on ngrok without https.
     * This is useful when we want to test the application on a mobile device.
     * Because we have no certificates, we need to disable some login features, https redirection,
     * and set a default user, to be able to test the application.
     */
    public static bool HostOnNgrokWithNoHttpsAndSetDefaultUser = false;
}
