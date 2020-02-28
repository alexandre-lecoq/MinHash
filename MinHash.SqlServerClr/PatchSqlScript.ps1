
function ConvertFileToHexString{
	param (
		[Parameter(Mandatory=$true)][string]$inputDllFilename
	)

	$inputDllContent = Get-Content $inputDllFilename -Encoding Byte ` -ReadCount 0 

	$dllHexDump = "0x"

	foreach ( $byte in $inputDllContent ) {
		$dllHexDump += "{0:X2}" -f $byte
	}

	return $dllHexDump;
}

$outputSqlFilename = ".\MinhashInstallation.sql"
$outputSqlContent = Get-Content $outputSqlFilename

$minhashHexContent = ConvertFileToHexString ".\MinHash.SqlServerClr.dll"
$outputSqlContent = $outputSqlContent -replace '_MINHASH_SQLSERVERCLR_DLL_HEX_', $minhashHexContent

$outputSqlContent | Set-Content -Path $outputSqlFilename

