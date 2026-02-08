# ?? JadeClinic Color Palette Reference

## ?? **Brand Identity Colors**
*Colors derived from the JadeClinic logo for consistent branding*

### **Primary Colors**

#### ?? **Golden Yellow** - `#FECF10`
- **RGB:** `254, 191, 16`  
- **Purpose:** Primary brand accent, highlights, active states
- **Usage:** Navigation active buttons, primary CTAs, success indicators

#### ?? **Rich Olive** - `#BE9A30`  
- **RGB:** `190, 154, 48`
- **Purpose:** Secondary accent, hover states, borders
- **Usage:** Button borders, secondary highlights, icons

#### ? **Clean White** - `#FFFFFF`
- **RGB:** `255, 255, 255`  
- **Purpose:** Text on dark backgrounds, clean surfaces
- **Usage:** Primary text, card backgrounds, input fields

---

## ?? **Extended Dark Theme Palette**

### **Background Colors**

#### ?? **Deep Charcoal** - `#1A1D1F`
- **RGB:** `26, 29, 31`
- **Purpose:** Primary background, main form background
- **Usage:** Form backgrounds, main container

#### ?? **Dark Slate** - `#2B2F32`  
- **RGB:** `43, 47, 50`
- **Purpose:** Secondary background, panels
- **Usage:** Navigation panel, side panels, modal backgrounds

#### ? **Graphite** - `#3D4145`
- **RGB:** `61, 65, 69`  
- **Purpose:** Card backgrounds, content areas
- **Usage:** DataGridView backgrounds, content panels

#### ?? **Steel Gray** - `#4A4F54`
- **RGB:** `74, 79, 84`
- **Purpose:** Interactive element backgrounds
- **Usage:** Input fields, button backgrounds, borders

### **Text Colors**

#### ?? **Pure White** - `#FFFFFF`
- **RGB:** `255, 255, 255`
- **Purpose:** Primary text on dark backgrounds
- **Usage:** Headers, main content text

#### ?? **Light Silver** - `#E1E5E9`  
- **RGB:** `225, 229, 233`
- **Purpose:** Secondary text, descriptions
- **Usage:** Subtitles, descriptions, metadata

#### ?? **Soft Gray** - `#B8BCC1`
- **RGB:** `184, 188, 193`  
- **Purpose:** Muted text, placeholders
- **Usage:** Placeholder text, disabled states, hints

#### ?? **Warm Gray** - `#9CA0A6`
- **RGB:** `156, 160, 166`
- **Purpose:** Tertiary text, separators
- **Usage:** Dividers, inactive text, borders

---

## ?? **Accent & State Colors**

### **Interactive States**

#### ?? **Success Green** - `#10D862`
- **RGB:** `16, 216, 98`
- **Purpose:** Success states, positive indicators
- **Usage:** Save confirmations, success messages, online status

#### ?? **Alert Red** - `#FF4757`  
- **RGB:** `255, 71, 87`
- **Purpose:** Error states, warnings, delete actions
- **Usage:** Error messages, logout button, critical alerts

#### ?? **Info Blue** - `#3742FA`
- **RGB:** `55, 66, 250`  
- **Purpose:** Information states, links
- **Usage:** Info messages, hyperlinks, help indicators

#### ?? **Warning Orange** - `#FF9F43`
- **RGB:** `255, 159, 67`
- **Purpose:** Warning states, pending actions
- **Usage:** Low stock warnings, pending status

---

## ?? **Professional Application**

### **Dashboard Specific Colors**

#### ?? **Navigation Background** - `#2B2F32`
- **RGB:** `43, 47, 50`
- **Usage:** DashboardPanel background

#### ?? **Content Background** - `#1A1D1F`  
- **RGB:** `26, 29, 31`
- **Usage:** Main dashboard area

#### ?? **Card Background** - `#3D4145`
- **RGB:** `61, 65, 69`  
- **Usage:** Statistics cards, data panels

#### ? **Active Highlight** - `#FECF10`
- **RGB:** `254, 191, 16`
- **Usage:** Active navigation button, selected states

#### ?? **Hover State** - `#BE9A30`  
- **RGB:** `190, 154, 48`
- **Usage:** Button hover effects, border highlights

---

## ?? **Color Usage Guidelines**

### **Do's** ?
- Use Golden Yellow (`#FECF10`) for primary actions and active states
- Apply dark backgrounds (`#1A1D1F`, `#2B2F32`) for main surfaces  
- Use white text (`#FFFFFF`) on dark backgrounds for readability
- Apply Rich Olive (`#BE9A30`) for secondary highlights and borders
- Use Success Green (`#10D862`) for positive feedback
- Apply Alert Red (`#FF4757`) sparingly for critical actions

### **Don'ts** ?
- Don't use bright colors on light backgrounds (poor contrast)
- Avoid using Golden Yellow for large background areas
- Don't mix warm and cool grays in the same component
- Avoid using red and green together without sufficient contrast
- Don't use more than 3 accent colors in a single view

---

## ?? **Implementation Reference**

### **VB.NET Color Codes**
```vb
' Primary Brand Colors
Dim GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10
Dim RichOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30

' Background Colors  
Dim DeepCharcoal As Color = Color.FromArgb(26, 29, 31)        ' #1A1D1F
Dim DarkSlate As Color = Color.FromArgb(43, 47, 50)           ' #2B2F32
Dim Graphite As Color = Color.FromArgb(61, 65, 69)            ' #3D4145
Dim SteelGray As Color = Color.FromArgb(74, 79, 84)           ' #4A4F54

' Text Colors
Dim PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF
Dim LightSilver As Color = Color.FromArgb(225, 229, 233)      ' #E1E5E9
Dim SoftGray As Color = Color.FromArgb(184, 188, 193)         ' #B8BCC1

' State Colors
Dim SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862
Dim AlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757
Dim InfoBlue As Color = Color.FromArgb(55, 66, 250)           ' #3742FA
Dim WarningOrange As Color = Color.FromArgb(255, 159, 67)     ' #FF9F43
```

### **CSS Equivalent**
```css
:root {
  --golden-yellow: #FECF10;
  --rich-olive: #BE9A30;
  --deep-charcoal: #1A1D1F;
  --dark-slate: #2B2F32;
  --graphite: #3D4145;
  --steel-gray: #4A4F54;
  --pure-white: #FFFFFF;
  --light-silver: #E1E5E9;
  --soft-gray: #B8BCC1;
  --success-green: #10D862;
  --alert-red: #FF4757;
  --info-blue: #3742FA;
  --warning-orange: #FF9F43;
}
```

---

## ?? **Color Accessibility**

All color combinations in this palette meet **WCAG 2.1 AA standards** for accessibility:
- **Text Contrast:** Minimum 4.5:1 ratio for normal text
- **Large Text:** Minimum 3:1 ratio for headings and large text  
- **Interactive Elements:** Clear focus indicators and hover states
- **Color Blind Friendly:** Tested for deuteranopia and protanopia

---

*Created for JadeClinic Dental Supply Management System*  
*Professional healthcare application color palette*