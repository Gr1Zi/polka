@echo off
cd /d "%~dp0"
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:winexe /codepage:65001 /out:Polka.exe Polka.cs
