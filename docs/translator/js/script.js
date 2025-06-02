let englishResourceStrings = {}; // To store { key: { value: '...', comment: '...' } }

document.addEventListener('DOMContentLoaded', () => {
  // Load english translation
  loadEnglishResw();

  // Load translations
  document.getElementById('loadTranslationBtn').addEventListener('click', () => {
    document.getElementById('reswFile').click();
  });
  document.getElementById('reswFile').addEventListener('change', handleUserFileSelect);

  // Save translations
  document.getElementById('saveTranslationBtn').addEventListener('click', saveTranslationFile);

});

async function loadEnglishResw() {
  // TODO: Update this after merge
  const url = 'https://raw.githubusercontent.com/beeradmoore/dlss-swapper/refs/heads/feature/translations-setup/src/Translations/en-US/Resources.resw';
  try {
    const response = await fetch(url);
    if (response.ok == false) {
      console.error('Failed to fetch English resw: ', response.statusText);
      // TODO: Should we link the users to report the issue?
      alert('Error: Could not load the base English translations. Please check the console for details.');
      return;
    }
    const xmlString = await response.text();
    const domParser = new DOMParser();
    const xmlDoc = domParser.parseFromString(xmlString, "text/xml");
    const errorNode = xmlDoc.querySelector("parsererror");
    if (errorNode) {
      console.error('Error parsing resw:', errorNode.textContent);
      alert('Error: Could not parse the selected resw file. Please check the console for details.');
      return;
    }

    const dataNodes = xmlDoc.getElementsByTagName("data");
    const tableBody = document.querySelector("table tbody");
    tableBody.innerHTML = '';

    // reset english resource strings
    englishResourceStrings = {};

    for (let i = 0; i < dataNodes.length; ++i) {
      const node = dataNodes[i];
      const name = node.getAttribute("name");
      if (!name) {
        continue;
      }

      const valueNode = node.getElementsByTagName("value")[0];
      const commentNode = node.getElementsByTagName("comment")[0];

      const englishText = valueNode ? valueNode.textContent : '';
      const commentText = commentNode ? commentNode.textContent : '';

      englishResourceStrings[name] = { value: englishText, comment: commentText };

      const row = tableBody.insertRow();
      row.setAttribute('data-key', name);

      const keyCell = row.insertCell();
      keyCell.textContent = name;

      if (commentText && commentText.trim() !== '') {
        const infoIcon = document.createElement('i');
        infoIcon.className = 'bi bi-info-circle-fill ms-2';
        infoIcon.setAttribute('data-bs-toggle', 'popover');
        infoIcon.setAttribute('data-bs-trigger', 'hover focus');
        infoIcon.setAttribute('data-bs-placement', 'top');
        infoIcon.setAttribute('data-bs-content', escapeXml(commentText));
        infoIcon.style.cursor = 'pointer';
        keyCell.appendChild(infoIcon);
      }

      const englishCell = row.insertCell();
      const translationCell = row.insertCell();

      const englishTextarea = document.createElement('textarea');
      englishTextarea.className = 'form-control';
      englishTextarea.value = englishText;
      englishTextarea.rows = 4;
      englishTextarea.readOnly = true;
      englishCell.appendChild(englishTextarea);

      const translationTextarea = document.createElement('textarea');
      translationTextarea.className = 'form-control';
      translationTextarea.placeholder = 'Enter translation';
      translationTextarea.rows = 4;
      translationCell.appendChild(translationTextarea);
    }

    // Initialize popovers fro comments
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
      return new bootstrap.Popover(popoverTriggerEl);
    });

  } catch (error) {
    console.error('Error fetching or parsing English resw:', error);
    alert('An unexpected error occurred while loading English translations. Please check the console for details.');
  }
}

function handleUserFileSelect(event) {
  const file = event.target.files[0];
  if (file) {
    const reader = new FileReader();
    reader.onload = function (e) {
      const userFileContent = e.target.result;
      parseUserResw(userFileContent);
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
    if (errorNode) {
      console.error('Error parsing resw:', errorNode.textContent);
      alert('Error: Could not parse the selected resw file. Please check the console for details.');
      return;
    }

    const dataNodes = xmlDoc.getElementsByTagName("data");
    
    // Reset translations
    const userTranslationResourceStrings = {};

    for (let i = 0; i < dataNodes.length; ++i) {
      const node = dataNodes[i];
      const name = node.getAttribute("name");
      if (!name) {
        continue;
      }
      const valueNode = node.getElementsByTagName("value")[0];
      const translationText = valueNode ? valueNode.textContent : '';
      
      if (translationText.length > 0) {
        userTranslationResourceStrings[name] = translationText;
      }
    }

    const tableRows = document.querySelectorAll("table tbody tr");
    tableRows.forEach(row => {
      const key = row.getAttribute('data-key');
      // Get teh cells from the third column
      const translationTextarea = row.cells[2] ? row.cells[2].querySelector('textarea') : null;
      if (translationTextarea && key) {
        if (userTranslationResourceStrings.hasOwnProperty(key)) {
          translationTextarea.value = userTranslationResourceStrings[key];
        } else {
          translationTextarea.value = '';
        }
      }
    });
  } catch (error) {
    console.error('Error processing user resw file:', error);
    alert('An unexpected error occurred while processing your resw file. Please check the console for more details.');
  }
}

// There is probably a much better way to do this.
function escapeXml(unsafe) {
  if (unsafe === null || typeof unsafe === 'undefined') return "";
  const strUnsafe = String(unsafe);
  return strUnsafe.replace(/[<>&'"]/g, function (c) {
    switch (c) {
      case '<': return '&lt;';
      case '>': return '&gt;';
      case '&': return '&amp;';
      case "\'": return '&apos;';
      case '"': return '&quot;';
    }
    return c;
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

  tableRows.forEach(row => {
    const key = row.getAttribute('data-key');
    if (!key || !englishResourceStrings[key]) {
      console.warn(`Skipping key (${key}) not found in original English strings or row has no key.`);
      return;
    }

    const translationTextarea = row.cells[2] ? row.cells[2].querySelector('textarea') : null;
    const translationText = translationTextarea ? translationTextarea.value.trim() : '';

    // Skip if translation text is empty
    if (translationText === '') {
      console.log(`Skipping key (${key}) because translation is empty.`);
      return;
    }

    const keyXml = escapeXml(key);
    const translationXml = escapeXml(translationText);

    reswBody += `  <data name="${keyXml}" xml:space="preserve">\n`;
    reswBody += `    <value>${translationXml}</value>\n`;
    reswBody += `  </data>\n`;
  });

  const reswContent = reswHeader + reswBody + `</root>`;

  const blob = new Blob([reswContent], { type: 'application/xml;charset=utf-8' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `Resources.translated.resw`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(link.href);
}
