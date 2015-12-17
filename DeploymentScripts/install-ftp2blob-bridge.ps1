#
# This script runs inside each Azure VM via the VMAgent. It sets up the VM from the inside
#
Param(
   [Parameter(Mandatory=$False)][string]$CloudServiceDNSName = "",
   [Parameter(Mandatory=$False)][string]$FTPPasvPort = "",
   [Parameter(Mandatory=$False)][string]$FtpAccountName = "",
   [Parameter(Mandatory=$False)][string]$FtpAccountKey = ""	 
)
Import-Module -Name ServerManager
# needed for the ftp server exe runtime
Install-WindowsFeature -Name "NET-Framework-45-Core"

function Unzip($zipfile, $targetdir) {
    if ( (test-path $targetdir) -eq $false ) {
	    mkdir $targetdir
    }
	$shell = new-object -com shell.application
	$zip = $shell.NameSpace($zipfile)
	foreach($item in $zip.items()) {
	 $shell.Namespace($targetdir).copyhere($item) 
	}
}

# get azftp2blob ftp server zip and create folders
$procdir = "c:\ftpdir"
mkdir $procdir
$exedir = "$procdir\release"
$logdir = "$procdir\log"
mkdir $logdir
$zipfile = "$procdir\azftp2blob.zip"

# copy averything that CSE downloaded to $procdir
$curdir = Split-Path -parent $PSCommandPath
copy $curdir\*.* $procdir

# unzip it
Unzip $zipfile $procdir

# update the app.config file with CloudService dns name, logpath and PASV port for ftp
$appConfigFile = "$exedir\azftp2blob.exe.config"
[xml]$appcfg = Get-Content $appConfigFIle
$appkey = $appcfg.selectNodes("/configuration/appSettings/add[@key='FTPPASV']")
$appkey[0].value = $FTPPasvPort
$appkey = $appcfg.selectNodes("/configuration/appSettings/add[@key='FtpServerHostPublic']")
$appkey[0].value = $CloudServiceDNSName
$appkey = $appcfg.selectNodes("/configuration/appSettings/add[@key='LogPath']")
$appkey[0].value = $logdir
$appkey = $appcfg.selectNodes("/configuration/appSettings/add[@key='StorageAccount']")
$appkey[0].value = "DefaultEndpointsProtocol=https;AccountName=$($FtpAccountName);AccountKey=$($FtpAccountKey)"
$appcfg.Save($appConfigFile)

# open ports for ftp
New-NetFirewallRule -DisplayName "Allow FTP In" -Direction Inbound -LocalPort 21 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "Allow FTPPASV In" -Direction Inbound -LocalPort $FTPPasvPort -Protocol TCP -Action Allow

# create a cmd file and execute it
$startupcmd = "$exedir\startup.cmd"

"@echo off" | out-file -filepath "$startupcmd" -Encoding ascii
":l01" | out-file -filepath "$startupcmd" -Encoding ascii -Append
"cd $exedir" | out-file -filepath "$startupcmd" -Encoding ascii -Append
"azftp2blob.exe" | out-file -filepath "$startupcmd" -Encoding ascii -Append
"goto l01" | out-file -filepath "$startupcmd" -Encoding ascii -Append

Start-Process cmd.exe -ArgumentList "/c $startupcmd"

# add scheduled task to launch the ftp server on server (re)boot
$ftpuser = "ftpuser"
$ftppwd = "F$(([System.Guid]::NewGuid()))".replace("-","").substring(0,14)  # generate a random password of 14 chars
net user $ftpuser $ftppwd /add 
net localgroup Administrators $ftpuser /add
schtasks /CREATE /TN "Start FTP Server" /SC ONSTART /RL HIGHEST /RU $ftpuser /RP $ftppwd /TR "cmd /c $startupcmd" /F 

