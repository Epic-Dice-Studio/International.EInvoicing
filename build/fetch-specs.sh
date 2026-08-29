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

EN16931_REF="validation-1.3.13"
PEPPOL_REF="master"
XRECHNUNG_SCHEMATRON_REF="master"
XRECHNUNG_TESTSUITE_REF="master"

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
    copy_licence "$src" "$SPECS_DIR/en16931"
}

fetch_peppol() {
    local src="$WORK_DIR/peppol"
    clone_at https://github.com/OpenPEPPOL/peppol-bis-invoice-3.git "$PEPPOL_REF" "$src"
    rm -rf "$SPECS_DIR/peppol/rules"
    sync_into "$src/rules/sch" "$SPECS_DIR/peppol/rules"
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

fetch_manual() {
    cat >&2 <<'MANUAL'

Redistributable, but published as archives rather than repositories — download and unpack them yourself,
then commit them:

  UBL 2.1 schemas          https://docs.oasis-open.org/ubl/os-UBL-2.1/            -> specs/ubl-2.1/
  UN/CEFACT CII D22B       https://unece.org/trade/uncefact/xml-schemas           -> specs/cii-d22b/
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
        peppol)    fetch_peppol ;;
        xrechnung) fetch_xrechnung ;;
        all)       fetch_en16931; fetch_peppol; fetch_xrechnung; fetch_manual ;;
        *)         warn "unknown target '$target' (en16931 | peppol | xrechnung | all)"; exit 2 ;;
    esac
    log "done — update the PROVENANCE.md of each folder you refreshed"
}

main "$@"
