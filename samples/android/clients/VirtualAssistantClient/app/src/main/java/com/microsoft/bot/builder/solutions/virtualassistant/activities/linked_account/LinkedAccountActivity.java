package com.microsoft.bot.builder.solutions.virtualassistant.activities.linked_account;

import android.content.Context;
import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.view.KeyEvent;
import android.view.View;
import android.webkit.CookieManager;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import com.microsoft.bot.builder.solutions.virtualassistant.R;
import com.microsoft.bot.builder.solutions.virtualassistant.activities.BaseActivity;

import java.net.URISyntaxException;

import butterknife.BindView;
import butterknife.ButterKnife;

public class LinkedAccountActivity extends BaseActivity {

    // VIEWS
    @BindView(R.id.linked_account)
    WebView linkedAccountWebView;

    // CONSTANTS
    private static final int CONTENT_VIEW = R.layout.activity_linked_account;
    private static final String SCHEME_MICROSOFT = "microsoft";

    public static Intent getNewIntent(Context context) {
        return new Intent(context, LinkedAccountActivity.class);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(CONTENT_VIEW);
        ButterKnife.bind(this);

        WebSettings webSettings = linkedAccountWebView.getSettings();
        webSettings.setJavaScriptEnabled(true);
        webSettings.setJavaScriptCanOpenWindowsAutomatically(true);
        linkedAccountWebView.setOnKeyListener(new View.OnKeyListener() {
            @Override
            public boolean onKey(View v, int keyCode, KeyEvent event) {
                if (event.getAction() == KeyEvent.ACTION_DOWN) {
                    if (keyCode == KeyEvent.KEYCODE_BACK && linkedAccountWebView.canGoBack()) {
                        linkedAccountWebView.goBack();
                        return true;
                    }
                }
                return false;
            }
        });

        CookieManager cookieManager = CookieManager.getInstance();
        cookieManager.setAcceptCookie(true);
        cookieManager.setAcceptThirdPartyCookies(linkedAccountWebView,true);

        linkedAccountWebView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                if (uri.getScheme().equals(SCHEME_MICROSOFT)) {
                    try {
                        Intent intent = Intent.parseUri(uri.toString(), Intent.URI_INTENT_SCHEME);
                        startActivity(intent);
                        finish();
                        return true;
                    } catch (URISyntaxException e) {
                        e.printStackTrace();
                    }
                }
                return super.shouldOverrideUrlLoading(view, request);
            }
        });

        linkedAccountWebView.loadUrl(configurationManager.getConfiguration().linkedAccountEndpoint + "/Home/LinkedAccounts?companionApp=True&userId=" + configurationManager.getConfiguration().userId);
    }

    @Override
    public void onBackPressed() {
        super.onBackPressed();
    }
}
