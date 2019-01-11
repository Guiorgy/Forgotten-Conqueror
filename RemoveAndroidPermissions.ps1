(Get-Content 'obj\Release\*\android\AndroidManifest.xml').replace('<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />', '') | Set-Content 'obj\Release\*\android\AndroidManifest.xml'
(Get-Content 'obj\Release\*\android\AndroidManifest.xml').replace('<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />', '') | Set-Content 'obj\Release\*\android\AndroidManifest.xml'

<#
    Don't forget to run Command Line (cmd) with administrator rights and execute:
    powershell Set-ExecutionPolicy Unrestricted -Scope CurrentUser
#>