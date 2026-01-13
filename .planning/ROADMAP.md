# Roadmap: CSV Import Wizard

## Overview

Enable Valt users to import transactions from CSV files through a guided wizard experience. The implementation progresses from CSV parsing infrastructure, through the multi-step wizard UI, to the import execution with background job integration.

## Domain Expertise

None

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: CSV Parser & Template** - Build CSV parsing service and sample template generator
- [x] **Phase 2: Import Wizard UI** - Create the 5-step wizard modal with account mapping and category preview
- [ ] **Phase 3: Import Execution** - Transaction creation, background job integration, and error handling

## Phase Details

### Phase 1: CSV Parser & Template
**Goal**: Parse CSV files with strict column format and generate sample template demonstrating all transaction types
**Depends on**: Nothing (first phase)
**Research**: Unlikely (CsvHelper already available, established patterns)
**Status**: Complete

Plans:
- [x] 01-01: CSV Parser & Template - CsvImportParser, CsvTemplateGenerator, unit tests

### Phase 2: Import Wizard UI
**Goal**: Create 5-step wizard modal (File Selection → Account Mapping → Category Preview → Summary → Import Progress)
**Depends on**: Phase 1
**Research**: Unlikely (existing modal/wizard patterns in codebase)
**Status**: Complete

Plans:
- [x] 02-01: Import Wizard Modal Infrastructure - ViewModel, View, registration, menu trigger
- [x] 02-02: Wizard Step Content - File selection, account mapping, category preview, summary, progress

### Phase 3: Import Execution
**Goal**: Implement transaction creation from parsed CSV data, integrate with background job system, handle errors
**Depends on**: Phase 2
**Research**: Unlikely (internal transaction creation, existing background job system)
**Status**: In progress

Plans:
- [x] 03-01: CsvImportExecutor Service - Interface, result types, account/category/transaction creation logic
- [ ] 03-02: ViewModel Integration & Tests - Connect UI to executor, background job pause/resume, unit tests

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. CSV Parser & Template | 1/1 | Complete | 2026-01-13 |
| 2. Import Wizard UI | 2/2 | Complete | 2026-01-13 |
| 3. Import Execution | 1/2 | In progress | - |
