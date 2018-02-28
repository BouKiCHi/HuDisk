CSC = C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
SRCS = Program.cs 
SRCS += HuDisk.cs HuBasicDiskImage.cs DataController.cs MiniOption.cs 
SRCS += DiskImage.cs HuFileEntry.cs SectorData.cs

TARGET = hudisk.exe

all : $(TARGET)

$(TARGET) : $(SRCS)
	$(CSC) /out:$(TARGET) $^

clean : 
	rm -f $(TARGET)