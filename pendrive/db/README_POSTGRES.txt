================================================================================
  Portable PostgreSQL - Setup Instructions for ABR USB Package
================================================================================

The ABR pendrive package does NOT include PostgreSQL (size and licensing).
You must copy the PostgreSQL Windows binaries into db\ once.

TARGET FOLDERS (all three are required, as siblings under db\)
----------------------------------------------------------------
  <USB drive>\db\bin\      Example: E:\db\bin\
  <USB drive>\db\share\    Example: E:\db\share\
  <USB drive>\db\lib\      Example: E:\db\lib\

IMPORTANT: bin\ alone is NOT enough. initdb reads shared catalog/timezone
data from share\ (postgres.bki, system_views.sql, timezone data, etc.) and
dynamically loads extension DLLs (e.g. dict_snowball) from lib\. Without
share\ and lib\ present as siblings of bin\, SETUP_FIRST_RUN.bat's initdb
step fails with errors like:
  FATAL: could not access file "dict_snowball": No such file or directory
This was hit and fixed during packaging - copy all three folders.

REQUIRED EXECUTABLES (in bin\)
-------------------------------
  initdb.exe
  pg_ctl.exe
  pg_isready.exe
  createdb.exe
  psql.exe
  pg_dump.exe

Also copy all .dll files from the PostgreSQL bin folder (libpq.dll, etc.).


OPTION A - Copy from an existing PostgreSQL installation
------------------------------------------------------------
  1. Install PostgreSQL on your development PC (if not already installed).
     Version 15+ works; a fresh initdb'd cluster has no cross-version
     migration concerns. Tested working with PostgreSQL 18.
     Download: https://www.postgresql.org/download/windows/

  2. Open the installation folder, typically:
     C:\Program Files\PostgreSQL\<version>\

  3. Copy the bin\, share\, and lib\ folders from there to:
     pendrive\package\db\bin\
     pendrive\package\db\share\
     pendrive\package\db\lib\
     (before copying package to USB)


OPTION B - Use EDB zip archive (no installer)
----------------------------------------------
  1. Download the PostgreSQL binaries zip for Windows x64 from EDB
  2. Extract the bin\, share\, and lib\ folder contents to
     pendrive\package\db\bin\, db\share\, db\lib\ respectively


VERIFY
------
  After copying, run from the package folder:
    01_VALIDATE_PACKAGE.bat

  All PostgreSQL checks should show [OK]. Note: 01_VALIDATE_PACKAGE.bat only
  checks for the bin\*.exe files; it does not check share\/lib\ presence,
  so if initdb fails during 02_SETUP_FIRST_RUN.bat, double check share\ and
  lib\ were copied too.


NOTES
-----
  - Port 5433 on the USB avoids conflicts with any PostgreSQL installed on
    the host PC at the default port 5432.
  - After SETUP_FIRST_RUN.bat, local connections require scram-sha-256 password
    authentication (no trust mode). The database password is stored only inside
    encrypted config\secrets.enc on the USB.
  - The database DATA lives in db\data\ (created by SETUP_FIRST_RUN.bat).
  - Do not copy db\data\ from another machine unless doing a full migration.

================================================================================
