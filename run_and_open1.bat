@echo off
chcp 65001 >nul
title Запуск Туристического путеводителя

echo ========================================
echo   ТУРИСТИЧЕСКИЙ ПУТЕВОДИТЕЛЬ
echo ========================================
echo.

:: Проверка наличия .NET
echo [1/3] Проверка .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ОШИБКА] .NET SDK не найден!
    echo Установите .NET SDK с https://dotnet.microsoft.com/download
    pause
    exit /b 1
)
echo [OK] .NET SDK найден
echo.

:: Переход в папку проекта
cd /d "%~dp0TouristGuide"

:: Проверка наличия папки проекта
if not exist "TouristGuide.csproj" (
    echo [ОШИБКА] Папка проекта не найдена!
    echo Убедитесь, что файл run_and_open1.bat лежит в папке PKS_PROJ4
    pause
    exit /b 1
)

:: Проверка и восстановление пакетов
echo [2/3] Проверка библиотек...
if exist "obj\project.assets.json" (
    echo [OK] Библиотеки уже установлены
) else (
    echo Восстановление библиотек...
    dotnet restore
    if %errorlevel% neq 0 (
        echo [ОШИБКА] Не удалось восстановить библиотеки!
        pause
        exit /b 1
    )
    echo [OK] Библиотеки установлены
)
echo.

:: Запуск сайта
echo [3/3] Запуск сайта...
echo.
echo ========================================
echo   Сайт запускается...
echo ========================================
echo.
echo Адреса сайта:
echo   https://localhost:5001
echo   http://localhost:5000
echo.
echo Нажмите Ctrl+C для остановки сервера
echo ========================================
echo.

:: Запуск с открытием браузера через 3 секунды
start "" cmd /c "timeout /t 3 /nobreak >nul && start https://localhost:5001"

:: Запуск сайта
dotnet run --urls "https://localhost:5001;http://localhost:5000"

:: Если сайт остановился
echo.
echo ========================================
echo   Сайт остановлен
echo ========================================
pause