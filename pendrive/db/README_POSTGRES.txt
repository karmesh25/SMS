================================================================================
  Portable PostgreSQL 15 - Setup Instructions for ABR USB Package
================================================================================

The ABR pendrive package does NOT include PostgreSQL (size and licensing).
You must copy PostgreSQL 15 Windows binaries into the db\bin\ folder once.

TARGET FOLDER
-------------
  <USB drive>\db\bin\
  Example: E:\db\bin\

REQUIRED EXECUTABLES
--------------------
  initdb.exe
  pg_ctl.exe
  pg_isready.exe
  createdb.exe
  psql.exe
  pg_dump.exe

Also copy all .dll files from the PostgreSQL bin folder (libpq.dll, etc.).


OPTION A - Copy from an existing PostgreSQL 15 installation
------------------------------------------------------------
  1. Install PostgreSQL 15 on your development PC (if not already installed)
     Download: https://www.postgresql.org/download/windows/

  2. Open the installation folder, typically:
     C:\Program Files\PostgreSQL\15\bin\

  3. Copy ALL files from that bin\ folder to:
     pendrive\package\db\bin\   (before copying package to USB)


OPTION B - Use EDB zip archive (no installer)
----------------------------------------------
  1. Download PostgreSQL 15 binaries zip for Windows x64 from EDB
  2. Extract the bin\ folder contents to pendrive\package\db\bin\


VERIFY
------
  After copying, run from the package folder:
    validate_package.bat

  All PostgreSQL checks should show [OK].


NOTES
-----
  - Use PostgreSQL 15 to match the application (port 5433 on USB avoids
    conflicts with any PostgreSQL installed on the PC at port 5432).
  - After SETUP_FIRST_RUN.bat, local connections require scram-sha-256 password
    authentication (no trust mode). The database password is stored only inside
    encrypted config\secrets.enc on the USB.
  - The database DATA lives in db\data\ (created by SETUP_FIRST_RUN.bat).
  - Do not copy db\data\ from another machine unless doing a full migration.

================================================================================
