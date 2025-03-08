# Manga Assistant - Project Reference

## Overview

Manga Assistant is a WPF application designed to manage your manga library and enhance it with metadata from AniList. The application scans your manga collection, organizes it into series and chapters, and fetches rich metadata including descriptions, genres, tags, and cover images from AniList.

## Project Purpose

- Manage a local manga library
- Scrape metadata and covers from AniList to populate local library metadata and files
- Track reading progress
- Provide a clean, organized interface for browsing manga collections

## Architecture

The solution follows a clean layered architecture with six main projects:

1. **MangaAssistant.Core**
   - Domain models (Series, Chapter, etc.)
   - Interfaces for all services
   - No dependencies on other projects

2. **MangaAssistant.Application**
   - Use cases/application services
   - Depends only on Core
   - Orchestrates business logic

3. **MangaAssistant.Infrastructure**
   - Implements Core interfaces
   - File system operations
   - Metadata providers
   - External API integrations

4. **MangaAssistant.Common**
   - Shared utilities
   - Extension methods
   - No business logic

5. **MangaAssistant.WPF**
   - Main WPF application
   - ViewModels
   - DI configuration

6. **MangaAssistant.WPF.Controls**
   - Reusable WPF controls
   - User controls
   - Custom dialogs

## Key Components

### Models

- **Series**: Represents a manga series with metadata, chapters, and progress tracking
- **Chapter**: Represents a single chapter file with reading status
- **SeriesMetadata**: Contains detailed metadata about a series (title, description, genres, etc.)
- **SeriesSearchResult**: Used for search results from metadata providers

### Services

- **LibraryService**: Manages the manga library, handles scanning and caching
- **LibraryScanner**: Scans directories for manga files, extracts metadata
- **MetadataService**: Orchestrates metadata providers and updates series metadata
- **AniListMetadataProvider**: Fetches metadata from AniList using GraphQL queries
- **SettingsService**: Manages application settings like library path

### ViewModels

- **MainViewModel**: Central ViewModel that coordinates UI updates and library operations
- **ViewModelLocator**: Handles dependency injection for ViewModels

### UI Components

- **MetadataSearchDialog**: UI for searching and selecting metadata from AniList

## Threading Model

Important threading pattern for WPF:
- All UI collection updates must happen on the Dispatcher thread
- Application.Current.Dispatcher.Invoke/InvokeAsync is used for UI updates
- Data is collected in temporary collections before updating UI collections
- Errors are handled on the UI thread for proper user feedback

## Data Flow

1. User sets a library path in settings
2. Application scans the directory for manga folders
3. Each folder is processed as a series, with CBZ files as chapters
4. User can search for metadata for each series
5. Metadata is fetched from AniList and stored locally
6. UI is updated with the enhanced metadata and cover images

## File Formats

- **CBZ**: Primary format for manga chapters
- **ComicInfo.xml**: Optional metadata within CBZ files
- **series-info.json**: Local metadata storage for each series
- **library-cache.json**: Cache of the entire library

## Development Guidelines

When making changes to the codebase:

1. **Maintain Architecture**: Respect the layered architecture and separation of concerns
2. **Thread Safety**: Follow the established threading pattern for WPF
3. **Error Handling**: Implement proper error handling and logging
4. **Code Quality**: Write clean, maintainable code with appropriate comments
5. **Testing**: Verify changes don't break existing functionality
6. **Check Existing Code**: Before creating new files, check if similar functionality already exists

## External Dependencies

- **AniList API**: GraphQL API for fetching manga metadata
- **.NET Framework**: Core framework for the application
- **WPF**: UI framework
