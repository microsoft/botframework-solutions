//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
package com.microsoft.bot.solutions.speechdevices.samples.botapp;

import android.os.Bundle;
import android.preference.ListPreference;
import android.preference.Preference;
import android.preference.PreferenceFragment;
import android.preference.PreferenceManager;
import android.view.MenuItem;

public class SettingsActivity extends AppCompatPreferenceActivity {
    private static final String TAG = SettingsActivity.class.getSimpleName();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        getSupportActionBar().setDisplayHomeAsUpEnabled(true);

        // load settings fragment
        getFragmentManager().beginTransaction().replace(android.R.id.content, new MainPreferenceFragment()).commit();
    }

    public static class MainPreferenceFragment extends PreferenceFragment {
        @Override
        public void onCreate(final Bundle savedInstanceState) {
            super.onCreate(savedInstanceState);
            addPreferencesFromResource(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.xml.pref_main);

            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_directline_secret)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_from_user_id)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_from_bot_id)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_latitude)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_longitude)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_devicegeometry)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_selectedgeometry)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_locale)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_gender)));
            bindPreferenceSummaryToValue(findPreference(getString(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.string.key_inputhint)));
        }
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        if (item.getItemId() == android.R.id.home) {
            onBackPressed();
        }
        return super.onOptionsItemSelected(item);
    }

    private static void bindPreferenceSummaryToValue(Preference preference) {
        preference.setOnPreferenceChangeListener(sBindPreferenceSummaryToValueListener);

        sBindPreferenceSummaryToValueListener.onPreferenceChange(preference,
                PreferenceManager
                        .getDefaultSharedPreferences(preference.getContext())
                        .getString(preference.getKey(), ""));
    }

    /**
     * A preference value change listener that updates the preference's summary
     * to reflect its new value.
     */
    private static Preference.OnPreferenceChangeListener sBindPreferenceSummaryToValueListener = new Preference.OnPreferenceChangeListener() {
        @Override
        public boolean onPreferenceChange(Preference preference, Object newValue) {
            String stringValue = newValue.toString();

            if (preference instanceof ListPreference) {
                // For list preferences, look up the correct display value in
                // the preference's 'entries' list.
                ListPreference listPreference = (ListPreference) preference;
                int index = listPreference.findIndexOfValue(stringValue);

                // Set the summary to reflect the new value.
                preference.setSummary(
                        index >= 0
                                ? listPreference.getEntries()[index]
                                : null);

            } else {
                preference.setSummary(stringValue);
            }

            return true;
        }
    };
}
