import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import en from './locales/en.json';
import es from './locales/es.json';
import fr from './locales/fr.json';
import zh from './locales/zh.json';

// Get language from URL query parameter
const getLanguageFromUrl = (): string | null => {
  const params = new URLSearchParams(window.location.search);
  // Check for Langcode parameter (case-insensitive)
  const langcode = params.get('Langcode') || params.get('langcode') || params.get('LangCode');
  if (langcode) {
    // Map common language codes
    const langMap: Record<string, string> = {
      'en': 'en',
      'es': 'es',
      'fr': 'fr',
      'cn': 'zh',
      'zh': 'zh',
      'zh-cn': 'zh',
      'zh-CN': 'zh',
    };
    return langMap[langcode.toLowerCase()] || langcode.substring(0, 2).toLowerCase();
  }
  return null;
};

const urlLanguage = getLanguageFromUrl();

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { translation: en },
      es: { translation: es },
      fr: { translation: fr },
      zh: { translation: zh },
    },
    lng: urlLanguage || undefined, // Use URL param if present, otherwise detect
    fallbackLng: 'en',
    interpolation: {
      escapeValue: false, // React already escapes values
    },
    detection: {
      order: ['querystring', 'localStorage', 'navigator'],
      lookupQuerystring: 'Langcode',
    },
  });

export default i18n;

// Helper to change language
export const changeLanguage = (lang: string) => {
  const langMap: Record<string, string> = {
    'cn': 'zh',
    'zh-cn': 'zh',
  };
  i18n.changeLanguage(langMap[lang.toLowerCase()] || lang);
};

// Available languages for UI
export const availableLanguages = [
  { code: 'en', name: 'English', flag: '🇺🇸' },
  { code: 'es', name: 'Español', flag: '🇪🇸' },
  { code: 'fr', name: 'Français', flag: '🇫🇷' },
  { code: 'zh', name: '中文', flag: '🇨🇳' },
];
