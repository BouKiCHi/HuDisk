
Write-Output "HuDisk test"
$imagePlainName = "TEST.2D"
$imageName = "TEST.D88"
$testSrcFile = "TEST_SRC.TXT"
$testFile = "TEST.TXT"

function testImage($image) {
    Write-Output ("testImage:" + $image)
    Copy-Item -Force $testSrcFile $testFile
    if (Test-Path $image) {
        Remove-Item -Force $image
    }
    ../hudisk -a $image $testFile
    Remove-Item .\$testFile
    ../hudisk -x $image $testFile
    $compare = Compare-Object (Get-Content $testSrcFile) (Get-Content $testFile)
    
    if ($compare -eq $NULL) {
        Write-Output "OK"
    }        
}

testImage $imageName
testImage $imagePlainName


