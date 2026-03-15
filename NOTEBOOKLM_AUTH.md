# NotebookLM Authentication Instructions

## Prerequisites
- Chrome browser installed
- Active Google account with NotebookLM access

## Step 1: Get Your Cookies

1. **Open Chrome** and navigate to: https://notebooklm.google.com
2. **Sign in** to your Google account if not already signed in
3. **Press F12** to open Developer Tools
4. **Click the 'Network' tab**
5. **In the filter box, type:** `batchexecute`
6. **Click on any notebook** in your NotebookLM to trigger a request
7. **Click on a 'batchexecute' request** in the Network list
8. **In the right panel, find 'Request Headers'**
9. **Find the line starting with 'cookie:'**
10. **Right-click the cookie VALUE** (the long string after "cookie: ") and select **'Copy value'**
11. **Paste the cookie string below** (replace this entire line with your cookie string)

## Your Cookie String (paste here):
PASTE_YOUR_COOKIE_STRING_HERE_REPLACE_THIS_ENTIRE_LINE

## Step 2: Run Authentication

After saving this file with your cookie string, run:
```bash
notebooklm-mcp-auth --file cookies.txt
```

## Step 3: Verify Installation

Once authenticated, test with:
```bash
notebooklm-mcp --help
```

Or use the MCP tool to list your notebooks.
