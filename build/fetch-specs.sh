#!/usr/bin/env bash
# Fetches the redistributable normative artefacts into specs/.
#
#   build/fetch-specs.sh            fetch everything
#   build/fetch-specs.sh en16931    fetch one standard
#
# Versions are pinned below. To upgrade a standard: change its ref, run this script, run the conformance
# tests, then record the new version and retrieval date in the folder's PROVENANCE.md.
#
# Artefacts that may not be redistributed are never downloaded here; the script prints where to get them.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SPECS_DIR="$REPO_ROOT/specs"
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

EN16931_REF="validation-1.3.16"
EN16931_COMPILED_REF="1.3.16"
PEPPOL_REF="master"
XRECHNUNG_SCHEMATRON_REF="master"
XRECHNUNG_TESTSUITE_REF="master"
PHIVE_RULES_REF="master"
FRENCH_RULES_VERSION="1.4.0.03"
FRENCH_FLUX10_VERSION="1.0"

log()  { printf '\033[1m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[33m!!\033[0m %s\n' "$*" >&2; }

clone_at() {
    local url="$1" ref="$2" target="$3"
    log "cloning $url @ $ref"
    git clone --depth 1 --branch "$ref" --quiet "$url" "$target"
}

sync_into() {
    local source="$1" destination="$2"
    [[ -e "$source" ]] || { warn "missing in upstream: $source"; return 1; }
    mkdir -p "$destination"
    cp -r "$source" "$destination/"
}

# Apache-2.0 requires the licence to travel with the artefacts it covers.
copy_licence() {
    local source="$1" destination="$2" name
    for name in LICENSE LICENSE.txt LICENSE.md COPYING; do
        if [[ -f "$source/$name" ]]; then
            cp "$source/$name" "$destination/LICENSE.upstream.txt"
            return 0
        fi
    done
    warn "no licence file found in $source"
}

fetch_en16931() {
    local src="$WORK_DIR/en16931"
    clone_at https://github.com/ConnectingEurope/eInvoicing-EN16931.git "$EN16931_REF" "$src"
    rm -rf "$SPECS_DIR/en16931/ubl" "$SPECS_DIR/en16931/cii"
    sync_into "$src/ubl/schematron" "$SPECS_DIR/en16931/ubl"
    sync_into "$src/cii/schematron" "$SPECS_DIR/en16931/cii"
    # The official examples: what the engine is measured against, rather than documents we wrote ourselves.
    sync_into "$src/ubl/examples" "$SPECS_DIR/en16931/ubl"
    sync_into "$src/cii/examples" "$SPECS_DIR/en16931/cii"
    copy_licence "$src" "$SPECS_DIR/en16931"
}

fetch_peppol() {
    local src="$WORK_DIR/peppol"
    clone_at https://github.com/OpenPEPPOL/peppol-bis-invoice-3.git "$PEPPOL_REF" "$src"
    rm -rf "$SPECS_DIR/peppol/rules" "$SPECS_DIR/peppol/examples" \
        "$SPECS_DIR/peppol/unit-UBL-PEPPOL" "$SPECS_DIR/peppol/unit-CII-PEPPOL"
    sync_into "$src/rules/sch" "$SPECS_DIR/peppol/rules"
    sync_into "$src/rules/examples" "$SPECS_DIR/peppol/examples"
    # Peppol's own unit corpus: each case names how many times a rule should fire. This is what the engine
    # is measured against, and it is stronger than any example document.
    sync_into "$src/rules/unit-UBL-PEPPOL" "$SPECS_DIR/peppol/unit-UBL-PEPPOL"
    sync_into "$src/rules/unit-CII-PEPPOL" "$SPECS_DIR/peppol/unit-CII-PEPPOL"
    copy_licence "$src" "$SPECS_DIR/peppol"
}

fetch_xrechnung() {
    local schematron="$WORK_DIR/xrechnung-schematron"
    local testsuite="$WORK_DIR/xrechnung-testsuite"
    clone_at https://github.com/itplr-kosit/xrechnung-schematron.git "$XRECHNUNG_SCHEMATRON_REF" "$schematron"
    clone_at https://github.com/itplr-kosit/xrechnung-testsuite.git "$XRECHNUNG_TESTSUITE_REF" "$testsuite"
    rm -rf "$SPECS_DIR/xrechnung/schematron" "$SPECS_DIR/xrechnung/testsuite"
    sync_into "$schematron/src" "$SPECS_DIR/xrechnung/schematron"
    copy_licence "$schematron" "$SPECS_DIR/xrechnung"
    sync_into "$testsuite/src" "$SPECS_DIR/xrechnung/testsuite"
}

# The French rule sets are published by the DGFiP and carried by phive-rules, which declares no licence.
# They are fetched into a git-ignored folder rather than redistributed.
fetch_france() {
    local src="$WORK_DIR/france"
    clone_at https://github.com/phax/phive-rules.git "$PHIVE_RULES_REF" "$src"

    local french="$src/phive-rules-france/src/test/resources/external"
    rm -rf "$SPECS_DIR/fr-dse/rules" "$SPECS_DIR/fr-dse/samples" "$SPECS_DIR/fr-dse/schemas"
    # Invoices and lifecycle messages.
    sync_into "$french/rule-source/ctc/$FRENCH_RULES_VERSION" "$SPECS_DIR/fr-dse/rules/ctc"
    # E-reporting, flux 10.
    sync_into "$french/rule-source/flux10/$FRENCH_FLUX10_VERSION" "$SPECS_DIR/fr-dse/rules/flux10"
    sync_into "$src/phive-rules-france/src/main/resources/external/schemas/flux10/$FRENCH_FLUX10_VERSION" \
        "$SPECS_DIR/fr-dse/schemas/flux10"
    # The DGFiP lifecycle samples: what the CDAR rules are measured against.
    sync_into "$french/test-files/ctc/$FRENCH_RULES_VERSION" "$SPECS_DIR/fr-dse/samples"
}

# Peppol PINT: the specification every Peppol jurisdiction outside Europe runs on. OpenPEPPOL publishes it
# under no redistribution licence, and phive-rules carries the artefacts as pre-compiled XSLT — which this
# library's engine cannot execute, so what is fetched here is what lets the identifiers be checked against
# their source, not a rule set that will run. See docs/standards/peppol-pint.md.
fetch_pint() {
    local src="$WORK_DIR/pint"
    clone_at https://github.com/phax/phive-rules.git "$PHIVE_RULES_REF" "$src"

    local pint="$src/phive-rules-peppol-pint/src/main/resources/external/schematron"
    rm -rf "$SPECS_DIR/peppol/pint"
    sync_into "$pint" "$SPECS_DIR/peppol/pint"

    # The same publisher's compiled EN 16931, of the version this repository ships as source. It is what
    # proves the compiled-Schematron reader: the two forms must yield the same assertions.
    rm -rf "$SPECS_DIR/en16931/compiled"
    sync_into "$src/phive-rules-en16931/src/main/resources/external/schematron/$EN16931_COMPILED_REF" \
        "$SPECS_DIR/en16931/compiled"
}

# National rule sets that their publishers ship only as compiled XSLT, aggregated by phive-rules. This
# library reads the compiled form (see docs/standards/peppol-pint.md), so these run like any other rule set.
# None of them is redistributable, so they are fetched into a git-ignored folder.
fetch_national() {
    local src="$WORK_DIR/national"
    clone_at https://github.com/phax/phive-rules.git "$PHIVE_RULES_REF" "$src"

    rm -rf "$SPECS_DIR/national"

    local module
    for module in simplerinvoicing cius-ro serbia turkey isdoc cius-pt zugferd ublbe eracun peppol-taxdata; do
        sync_into "$src/phive-rules-$module/src/main/resources/external/schematron" \
            "$SPECS_DIR/national/$module"
    done
}

# The UBL 2.1 schemas, file by file rather than as the 58 MB OASIS archive: what a validator needs is the
# xsd folder, and the rest of that zip is documentation and examples. OASIS publishes them individually.
fetch_ubl_schemas() {
    local base="https://docs.oasis-open.org/ubl/os-UBL-2.1/xsd"
    local target="$SPECS_DIR/ubl-2.1/xsd"
    local name

    log "fetching UBL 2.1 schemas"
    mkdir -p "$target/maindoc" "$target/common"

    for name in UBL-Invoice-2.1 UBL-CreditNote-2.1 UBL-ApplicationResponse-2.1; do
        curl -fsS "$base/maindoc/$name.xsd" -o "$target/maindoc/$name.xsd"
    done

    for name in \
        UBL-CommonAggregateComponents-2.1 UBL-CommonBasicComponents-2.1 UBL-CommonExtensionComponents-2.1 \
        UBL-ExtensionContentDataType-2.1 UBL-QualifiedDataTypes-2.1 UBL-UnqualifiedDataTypes-2.1 \
        UBL-CommonSignatureComponents-2.1 UBL-SignatureAggregateComponents-2.1 UBL-SignatureBasicComponents-2.1 \
        UBL-XAdESv132-2.1 UBL-XAdESv141-2.1 UBL-xmldsig-core-schema-2.1 CCTS_CCT_SchemaModule-2.1; do
        curl -fsS "$base/common/$name.xsd" -o "$target/common/$name.xsd"
    done
}

# The UN/CEFACT CII D22B schemas. UNECE publishes them as an archive; phax/ph-cii carries the same files
# unpacked, which is what makes them fetchable one by one. Free to use and redistribute, per UN/CEFACT.
fetch_cii_schemas() {
    local base="https://raw.githubusercontent.com/phax/ph-cii/master/ph-cii-d22b/src/main/resources/external/schemas/d22b/cii"
    local target="$SPECS_DIR/cii-d22b/xsd"
    local listing="$WORK_DIR/ph-cii.json"
    local path name

    log "fetching UN/CEFACT CII D22B schemas"
    mkdir -p "$target"

    curl -fsS "https://api.github.com/repos/phax/ph-cii/git/trees/HEAD?recursive=1" -o "$listing"

    while read -r path; do
        name="$(basename "$path")"
        curl -fsS "$base/$name" -o "$target/$name"
    done < <(grep -o '"path": "[^"]*/d22b/cii/[^"]*\.xsd"' "$listing" | cut -d'"' -f4)
}

fetch_manual() {
    cat >&2 <<'MANUAL'

Redistributable, but published as archives rather than repositories — download and unpack them yourself,
then commit them:

  UN/CEFACT CDAR           https://unece.org/trade/uncefact/xml-schemas           -> specs/cdar/
  Factur-X schemas/samples https://fnfe-mpe.org                                   -> specs/facturx/

Not redistributable — download for your own use, never commit:

  Factur-X specification   https://fnfe-mpe.org
  French DGFiP B2B specs   https://www.impots.gouv.fr/specifications-externes-b2b
  EN 16931 standard text   AFNOR / DIN / BSI

MANUAL
}

main() {
    local target="${1:-all}"
    case "$target" in
        en16931)   fetch_en16931 ;;
        pint)      fetch_pint ;;
        national)  fetch_national ;;
        peppol)    fetch_peppol ;;
        xrechnung) fetch_xrechnung ;;
        ubl)       fetch_ubl_schemas ;;
        cii)       fetch_cii_schemas ;;
        france)    fetch_france ;;
        all)       fetch_en16931; fetch_peppol; fetch_pint; fetch_national; fetch_xrechnung; fetch_france; fetch_ubl_schemas; fetch_cii_schemas; fetch_manual ;;
        *)         warn "unknown target '$target' (en16931 | peppol | pint | national | xrechnung | france | all)"; exit 2 ;;
    esac
    log "done — update the PROVENANCE.md of each folder you refreshed"
}

main "$@"
