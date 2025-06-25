let englishResourceStrings = {}; // To store { key: { value: '...', comment: '...' } }

document.addEventListener("DOMContentLoaded", () => {
  // Load English translation and then init auto-save if enabled
  loadEnglishResw().then(() => {
    const autoSaveCheckbox = document.getElementById("autoSave");
    let isEnabled = localStorage.getItem("autoSaveEnabled");
    if (isEnabled === null) {
      isEnabled = true; // default: true
      localStorage.setItem("autoSaveEnabled", "true");
    } else {
      isEnabled = isEnabled === "true";
    }
    autoSaveCheckbox.checked = isEnabled;

    if (isEnabled) {
      loadAutoSavedTranslations();
    }

    bindAutoSaveListeners();

    autoSaveCheckbox.addEventListener("change", handleAutoSaveToggle);
  });

  // Load translations
  document
    .getElementById("loadTranslationBtn")
    .addEventListener("click", () => {
      document.getElementById("reswFile").click();
    });

  document
    .getElementById("reswFile")
    .addEventListener("change", handleUserFileSelect);

  // Save translations
  document
    .getElementById("saveTranslationBtn")
    .addEventListener("click", saveTranslationFile);
});

async function loadEnglishResw() {
  const url =
    "https://raw.githubusercontent.com/beeradmoore/dlss-swapper/refs/heads/feature/translations-setup/src/Translations/en-US/Resources.resw";
  try {
    const response = await fetch(url);
    if (!response.ok) throw new Error(response.statusText);

    const xmlString = await response.text();
    const domParser = new DOMParser();
    const xmlDoc = domParser.parseFromString(xmlString, "text/xml");

    const errorNode = xmlDoc.querySelector("parsererror");
    if (errorNode) throw new Error(errorNode.textContent);

    const dataNodes = xmlDoc.getElementsByTagName("data");
    const tableBody = document.querySelector("table tbody");
    tableBody.innerHTML = "";
    englishResourceStrings = {};

    for (let i = 0; i < dataNodes.length; ++i) {
      const node = dataNodes[i];
      const name = node.getAttribute("name");
      if (!name) continue;

      const valueNode = node.getElementsByTagName("value")[0];
      const commentNode = node.getElementsByTagName("comment")[0];

      const englishText = valueNode ? valueNode.textContent : "";
      const commentText = commentNode ? commentNode.textContent : "";

      englishResourceStrings[name] = {
        value: englishText,
        comment: commentText,
      };

      const row = tableBody.insertRow();
      row.setAttribute("data-key", name);

      const keyCell = row.insertCell();
      keyCell.textContent = name;

      if (commentText.trim()) {
        const infoIcon = document.createElement("i");
        infoIcon.className = "bi bi-info-circle-fill ms-2";
        infoIcon.setAttribute("data-bs-toggle", "popover");
        infoIcon.setAttribute("data-bs-trigger", "hover focus");
        infoIcon.setAttribute("data-bs-placement", "top");
        infoIcon.setAttribute("data-bs-content", escapeXml(commentText));
        infoIcon.style.cursor = "pointer";
        keyCell.appendChild(infoIcon);
      }

      const englishCell = row.insertCell();
      const translationCell = row.insertCell();

      const englishTextarea = document.createElement("textarea");
      englishTextarea.className = "form-control";
      englishTextarea.value = englishText;
      englishTextarea.rows = 4;
      englishTextarea.readOnly = true;
      englishCell.appendChild(englishTextarea);

      const translationTextarea = document.createElement("textarea");
      translationTextarea.className = "form-control";
      translationTextarea.placeholder = "Enter translation";
      translationTextarea.rows = 4;
      translationCell.appendChild(translationTextarea);
    }

    // Initialize popovers
    const popoverTriggerList = [].slice.call(
      document.querySelectorAll('[data-bs-toggle="popover"]')
    );
    popoverTriggerList.map((el) => new bootstrap.Popover(el));
  } catch (error) {
    console.error("Error loading English resw:", error);
    alert("Failed to load English translations. Check console for details.");
  }
}

function handleUserFileSelect(event) {
  const file = event.target.files[0];
  if (file) {
    const reader = new FileReader();
    reader.onload = function (e) {
      parseUserResw(e.target.result);
    };
    reader.onerror = function () {
      console.error("Error reading file:", reader.error);
      alert("Error reading the selected file.");
    };
    reader.readAsText(file);
  }
}

function parseUserResw(xmlString) {
  try {
    const parser = new DOMParser();
    const xmlDoc = parser.parseFromString(xmlString, "text/xml");

    const errorNode = xmlDoc.querySelector("parsererror");
    if (errorNode) throw new Error(errorNode.textContent);

    const dataNodes = xmlDoc.getElementsByTagName("data");
    const userTranslationResourceStrings = {};

    for (let i = 0; i < dataNodes.length; ++i) {
      const node = dataNodes[i];
      const name = node.getAttribute("name");
      const valueNode = node.getElementsByTagName("value")[0];
      if (name && valueNode) {
        userTranslationResourceStrings[name] = valueNode.textContent;
      }
    }

    const tableRows = document.querySelectorAll("table tbody tr");
    tableRows.forEach((row) => {
      const key = row.getAttribute("data-key");
      const translationTextarea = row.cells[2]?.querySelector("textarea");
      if (key && translationTextarea) {
        translationTextarea.value = userTranslationResourceStrings[key] || "";
      }
    });
  } catch (error) {
    console.error("Error parsing user resw file:", error);
    alert("Failed to parse the selected resw file.");
  }
}

function escapeXml(unsafe) {
  if (unsafe === null || typeof unsafe === "undefined") return "";
  return String(unsafe).replace(/[<>&'"]/g, (c) => {
    switch (c) {
      case "<":
        return "&lt;";
      case ">":
        return "&gt;";
      case "&":
        return "&amp;";
      case "'":
        return "&apos;";
      case '"':
        return "&quot;";
      default:
        return c;
    }
  });
}

function saveTranslationFile() {
  const reswHeader = `<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>1.3</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
`;

  let reswBody = "";
  const tableRows = document.querySelectorAll("table tbody tr");
  tableRows.forEach((row) => {
    const key = row.getAttribute("data-key");
    const translationTextarea = row.cells[2]?.querySelector("textarea");
    const translationText = translationTextarea?.value.trim() || "";

    if (key && englishResourceStrings[key] && translationText !== "") {
      const keyXml = escapeXml(key);
      const translationXml = escapeXml(translationText);
      reswBody += `  <data name="${keyXml}" xml:space="preserve">\n`;
      reswBody += `    <value>${translationXml}</value>\n`;
      reswBody += `  </data>\n`;
    }
  });

  const reswContent = reswHeader + reswBody + `</root>`;

  const blob = new Blob([reswContent], {
    type: "application/xml;charset=utf-8",
  });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = `Resources.translated.resw`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(link.href);
}

///////////////////////////////////////////////////////////////
// Auto Save logic

function autoSaveTranslations() {
  const data = {};
  const tableRows = document.querySelectorAll("table tbody tr");
  tableRows.forEach((row) => {
    const key = row.getAttribute("data-key");
    const textarea = row.cells[2]?.querySelector("textarea");
    if (key && textarea) {
      data[key] = textarea.value;
    }
  });
  localStorage.setItem("translationsAutoSave", JSON.stringify(data));
}

function loadAutoSavedTranslations() {
  const savedData = localStorage.getItem("translationsAutoSave");
  if (!savedData) return;
  const translations = JSON.parse(savedData);
  const tableRows = document.querySelectorAll("table tbody tr");
  tableRows.forEach((row) => {
    const key = row.getAttribute("data-key");
    const textarea = row.cells[2]?.querySelector("textarea");
    if (key && textarea && translations[key] !== undefined) {
      textarea.value = translations[key];
    }
  });
}

function bindAutoSaveListeners() {
  const tableRows = document.querySelectorAll("table tbody tr");
  tableRows.forEach((row) => {
    const textarea = row.cells[2]?.querySelector("textarea");
    if (textarea) {
      textarea.addEventListener("input", () => {
        const autoSaveEnabled = document.getElementById("autoSave").checked;
        if (autoSaveEnabled) autoSaveTranslations();
      });
    }
  });
}

function handleAutoSaveToggle() {
  const autoSaveCheckbox = document.getElementById("autoSave");
  const isChecked = autoSaveCheckbox.checked;
  localStorage.setItem("autoSaveEnabled", isChecked);
  if (!isChecked) {
    localStorage.removeItem("translationsAutoSave");
  }
}
