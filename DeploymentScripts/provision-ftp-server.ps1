#
# This script runs on your local devops machine
#
Param(
   [Parameter(Mandatory=$False)][string]$CloudService = "...",
   [Parameter(Mandatory=$False)][string]$VMBaseName = "...",
   [Parameter(Mandatory=$False)][int]$CountVM = "2",
   [Parameter(Mandatory=$False)][string]$AdminUid = "",
   [Parameter(Mandatory=$False)][string]$AdminPwd = ""
)
# ----------------------------------------------------------------------------
# variables that needs to be changed 

# name the default StorageAccount (where the VHD files go for the VMs)
$StgAccount = "..."   

# url root to blob storage where you store the CustomScriptExtensions
# it is the install tion powershell script that runs inside each VM during setup phase
# and the zip file that contains the binaris
$CSERootUrl = "https://$($StgAccount).blob.core.windows.net/windows-powershell-dsc"

# what storage account to use for ftp server storage - will update the app.config file by the CSE
$FtpAccountName = "..."
$FtpAccountKey = "..."

$Location = "West Europe"
$InstanceSize = "Standard_D1"

# ----------------------------------------------------------------------------
# if pwd specified on cmdline, ask for uid/pwd here (good when demoing on a projector)
if ( $AdminPwd -eq "" ) {
	$Credential = Get-Credential $AdminUid
	$AdminUid = $Credential.UserName
	$AdminPwd = $Credential.GetNetworkCredential().password
}
# ----------------------------------------------------------------------------
# setup the subscription to use

$SubscriptionName = ""         # name the Azure Subscription you are using (leave blank for default)
if ( $SubscriptionName -eq "" ) {
	$SubscriptionName = (get-azuresubscription | where { $_.IsDefault -eq $True } | select SubscriptionName).SubscriptionName
} else {
	Select-AzureSubscription -SubscriptionName $SubscriptionName 
}

Write-Verbose "Setting Default StorageAccount: $StgAccount"
Set-AzureSubscription -SubscriptionName $SubscriptionName -CurrentStorageAccount $StgAccount

# ----------------------------------------------------------------------------
# get all images once since it's a time consuming call
Write-Verbose "Getting Images from Gallary"
$images = Get-AzureVMImage

# get the latest image from the image gallary
$Image = $images | where { $_.ImageFamily -eq "Windows Server 2012 R2 Datacenter" } | sort PublishedDate -Descending | select -First 1 
Write-Verbose "OS: $($image.OS), Image: $($Image.ImageName)"

# ---------------------------------------------------------------------------
# Process all VMs in the Cloud Service
$AvailabilitySetName = $CloudService
$timeZone = [System.TimeZoneInfo]::Local.Id

# VM names we want to create. Add additional for more
$vmS = [System.Collections.ArrayList]@()
for( $i = 1; $i -le $CountVM; $i++) {
	$vmS.Add("$VMBaseName$i")
}
# create a non-fixed array to hold all VMs in the Cloud Service - provision as a one time thing
$vmList = [System.Collections.ArrayList]@()

# ftp server program zipped - this is the ftp server exe and binaries, etc. Gets downloaded by the CSE below
$FtpServerZip = "$($CSERootUrl)/azftp2blob.zip"

# Custom Script Extensions - location of the powershell script you want to run at VM creation time
# this script installs everything you need on the VM, incl the ftp server
$CseFileName = "install-ftp2blob-bridge.ps1"
$CseFileUrl = "$($CSERootUrl)/$CseFileName", $FtpServerZip

# PASV ftp port. Will increment +1 for each VM so that they have a one each
$pasvPort = 59860 

foreach( $VM in $vmS) {
	$pasvPort = $pasvPort + 1 # increment 

	$vmC = get-azurevm | where { $_.Name -eq $VM.Name }
	if ( $vmC -ne $null ) {
		Write-Host "VM $($VM.Name) already exists..."
		continue
	}

	# create the VM config in a predefined subnet - with/out a High Availability Set name
	Write-Verbose "Setting VM Config for VM $VM with HighAvailabilitySet $AvailabilitySetName..."
	$vmwe1 = New-AzureVMConfig -ImageName $Image.ImageName -Name $VM -InstanceSize $InstanceSize -AvailabilitySetName $AvailabilitySetName

	Write-Verbose "Setting Windows userid: $adminUid and TimeZone $timeZone"
	$vmwe1 = Add-AzureProvisioningConfig -VM $vmwe1 -Windows -AdminUsername $adminUid -Password $adminPwd -TimeZone $timeZone

	# set the endpoint for FTP port 21 that is load balanced
	Write-Verbose "Adding load balanced Endpoint FTP on port 21"
	$vmwe1 = Add-AzureEndpoint -VM $vmwe1 -Name "FTP" -Protocol "TCP" -LocalPort 21 -PublicPort 21 -LBSetName "FTPLB" -DefaultProbe 

	# set up endpoint for PASV ftp. It is not load balanced and unique for each VM
	Write-Verbose "Adding Endpoint FTPPASV on port $pasvPort"
	$vmwe1 = Add-AzureEndpoint -VM $vmwe1 -Name "FTPPASV" -Protocol "TCP" -LocalPort $pasvPort -PublicPort $pasvPort

    # specify Custom Script Extension so we can modify the VM at deployment
    if ( $CseFileName -ne $null ) {
		Write-Verbose "Setting Windows Custom Script Extension $CseFileUrl"
		$vmwe1 = Set-AzureVMCustomScriptExtension -VM $vmwe1 -FileUri $CseFileUrl `
				-Run $CseFileName -Argument "$($CloudService).cloudapp.net $pasvPort $FtpAccountName $FtpAccountKey"
    }
	# add the VM to the list of VMs
	$vmList.Add($vmwe1)
} # VM

# ---------------------------------------------------------------------------
# create the VM(s) in the Cloud Service
if ( $vmList.Count -gt 0 ) {
	Write-Verbose "Provisioning Cloud Service $CloudService in Location $Location)"
	New-AzureVM -ServiceName $CloudService -Location $Location -VMs $vmList
}

# To delete everything, you do
Write-Host "To completely delete the deployment, issue command"
write-host "Remove-AzureService -ServiceName $CloudService -DeleteAll -Force"
