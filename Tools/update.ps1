#oproep script powershell -ExecutionPolicy Unrestricted -f test.ps1 -rubypath "C:\dennis\from" -checkFile "test.rb"

param (
        [string]$drive = (Get-Location).Drive.Name + ":/",
        [string]$rubyPath = "/opt/sensu/embedded/bin/ruby.exe",
        [string]$rubyArguments = "-C", #Currently not used
        [string]$pluginsPath = "/opt/sensu-plugins/",
        [string]$checkFile,
        [string]$powershellProgram = "powershell.exe",
        [string]$powershellArguments = " -ExecutionPolicy Unrestricted -f ",
        [string]$checkDownLoadURL = "http://agent.monitor.trancon.nl/", #TODO, only for testing, change to sensu-2 server URL. This URL is also used to review if Last-Modified and check wether the file can be downloaded.
        [long]$lastUpdateRunInTicks = 0,
        [int]$daysAfterNextVersionCheck = 0,
        [string]$ruby = "",
        [string]$powershell = "",
        [string]$http = "",
        [string]$arguments = "",
        [string]$command = "" #Override the whole command
        )

Function Set-Drive-Path($drive, $path) {
    if ($path -like '*:*') {
        return $path
    }
    $result = Join-Path $drive $path 
    return $result
}

#define script type(ruby. powershell, etc.)
Function Set-Script-Command($checkFile, $command) {
    if ($checkFile -Match ".ps1") {
        $command = "$powershellProgram $powershellArguments $pluginsPath$checkFile $arguments"
    } elseif ($checkFile -Match ".rb") {
        if( -not(Test-Path "$FrubyPath")) {
            throw [System.IO.IOException] " Ruby path: $($FrubyPath) does not exist"
        }
        $rubyCheckFile = " $checkFile"
        $command = "$FrubyPath $rubyArguments $FpluginsPath $rubyCheckFile $arguments"
        Write-Host "command " + $command
    } elseif ($command.Length -ge 1 -and $checkFile.Length -le 0) {
        $command = $command
    }
    return $command
}

#check if the file exists, when it doesn't exists it should be downloaded
Function Check-File-Version($FpluginsPath, $checkFile, $checkDownLoadURL, $daysAfterNextCheck) {
    [long]$nSPerSecond = 10000000
    [System.DateTime]$todayDt = [System.DateTime]::Now
    [long]$ticksAfterNextCheck = $daysAfterNextCheck * 60 * 60 * 24 * $nSPerSecond
    $localFileLastModified = ([System.IO.FileInfo]"$pluginsPath$checkFile").LastWriteTime
    $ticksToday = $todayDt.Ticks
    $timeInTicksLastModified = $ticksToday - ($localFileLastModified.Ticks)
    Write-Host $ticksToday
    Write-Host $localFileLastModified.Ticks

    if( -not(Test-Path "$FpluginsPath$checkFile")) {
        if( -not(Test-Path "$FpluginsPath")) {
            throw [System.IO.IOException] " Plugin path does not exist"
        }

        try {
            $webRequest = New-Object System.net.WebClient
            $webRequest.DownloadFile("$checkDownLoadURL$checkFile", "$FpluginsPath$checkFile")
        } catch [System.Net.WebException] {
            $Status = $_.Exception.Response.StatusCode
            throw [System.Net.WebException] "  Error dowloading $checkFile for the first time, Status code: $Status - $msg"
        }
    } elseif ($ticksAfterNextCheck -le $timeInTicksLastModified) { #DOES NOT WORK, NEEDS TIME LAST EXECUTED------------------------------------------------------------------------------------------------------------------------------------------
        try {
            
            Write-Host $timeInTicksLastModified 
            Write-Host $ticksAfterNextCheck
            Write-Host ($ticksAfterNextCheck -le $timeInTicksLastModified)
            
		    #use HttpWebRequest to check modified since
		    $webRequest = [System.Net.HttpWebRequest]::Create("$checkDownLoadURL");

            #use modifiedSince from the download url to check if the file has changed
		    $webRequest.IfModifiedSince = $localFileLastModified
		    $webRequest.Method = "HEAD"
		    [System.Net.HttpWebResponse]$webResponse = $webRequest.GetResponse()

            #use HttpWebRequest again to download file when the head request didn't respond with a 304 response
		    $webRequest = [System.Net.HttpWebRequest]::Create("$checkDownLoadURL$checkFile");
		    $webRequest.Method = "GET"
		    [System.Net.HttpWebResponse]$webResponse = $webRequest.GetResponse()

		    #Read HTTP result
		    $stream = New-Object System.IO.StreamReader($webResponse.GetResponseStream())

		    #Save to file
		    $stream.ReadToEnd() | Set-Content -Path "$FpluginsPath$checkFile" -Force 
	    } catch [System.Net.WebException] {
		    #Check for a 304
		    if ($_.Exception.Response.StatusCode -eq [System.Net.HttpStatusCode]::NotModified) {
                Write-Host "Not modified, not downloaded...."
		    } else {
			    #Unexpected 
			    $Status = $_.Exception.Response.StatusCode
			    $msg = $_.Exception
			    throw [System.Net.WebException] "  Error dowloading $checkFile when updating, Status code: $Status - $msg"
		    }
	    }
     }
}

#check if the file exists, when it doesn't exists it should be downloaded. The check is checked with a custom command TODO--------------------------------------------------------------------------------
Function Check-File-Version-By-Command($pluginsPath, $checkFile, $checkDownLoadURL, $daysAfterNextCheck) {
}

Function Run-Command($commandLine) {
    #Run cmd 
            Write-Host "Run command: " $commandLine
            Invoke-Expression "$commandLine"
}

Function Main {
    Process {
        $FrubyPath = Set-Drive-Path $drive $rubyPath
        $FpluginsPath = Set-Drive-Path $drive $pluginsPath
        $Fcommand = Set-Script-Command $checkFile $command
        Check-File-Version $FpluginsPath $checkFile $checkDownLoadURL $daysAfterNextVersionCheck
        Run-Command $Fcommand
    }
}

Main
