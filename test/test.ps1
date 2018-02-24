
Write-Output "HuDisk test"
$testSrcFile = "TEST_SRC.TXT"
$testFile = "TEST.TXT"

function testImage($image) {
    Write-Output ("testImage:" + $image)
    Copy-Item -Force $testSrcFile $testFile
    if (Test-Path $image) {
        Remove-Item $image
    }
    ../hudisk -a $image $testFile
    Remove-Item .\$testFile
    ../hudisk -x $image $testFile
    $compare = Compare-Object (Get-Content $testSrcFile) (Get-Content $testFile)
    
    if ($compare -eq $NULL) {
        Write-Output "OK"
    }        
}

function testBoundary($image) {
    Write-Output ("boundImage:" + $image)
    if (Test-Path $image) {
        Remove-Item $image
    }
    ../hudisk -a $image "255.bin"
    ../hudisk -a $image "256.bin"
    ../hudisk -a $image "257.bin"
    ../hudisk -a $image "512.bin"
    ../hudisk -a $image "555.bin"
    ../hudisk -l $image 
}

function inOutTest() {
    $imagePlainName = "TEST.2D"
    $imageName = "TEST.D88"

    testImage $imageName
    testImage $imagePlainName
}

function boundTest() {
    testBoundary "BOUND.D88"
}

boundTest




