# GitHub remote setup (run once after `gh auth login`)

After authenticating with GitHub CLI:

```powershell
cd F:\SMS
gh repo create abr-sms --public --source=. --remote=origin --description "ABR Society & Real Estate Management System - Angular + ASP.NET Core monorepo"
git push -u origin main
```

Replace `abr-sms` with your preferred repository name and use `--private` if needed.

If the repository already exists on GitHub:

```powershell
git remote add origin https://github.com/YOUR_USERNAME/abr-sms.git
git push -u origin main
```
