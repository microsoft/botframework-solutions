package com.microsoft.bot.builder.solutions.virtualassistant.activities.main.chatlist;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Canvas;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.util.Base64;

import com.pixplicity.sharp.Sharp;

import java.io.IOException;
import java.net.URISyntaxException;

import io.adaptivecards.objectmodel.AdaptiveBase64Util;
import io.adaptivecards.objectmodel.CharVector;
import io.adaptivecards.renderer.GenericImageLoaderAsync;
import io.adaptivecards.renderer.IResourceResolver;
import io.adaptivecards.renderer.Util;
import io.adaptivecards.renderer.http.HttpRequestResult;

public class SvgImageLoader implements IResourceResolver
{
    @Override
    public HttpRequestResult<Bitmap> resolveImageResource(String uri, GenericImageLoaderAsync genericImageLoaderAsync) throws IOException, URISyntaxException
    {
        Bitmap bitmap;
        String dataUri = AdaptiveBase64Util.ExtractDataFromUri(uri);
        CharVector decodedDataUri = AdaptiveBase64Util.Decode(dataUri);
        byte[] decodedByteArray = Util.getBytes(decodedDataUri);
        bitmap = BitmapFactory.decodeByteArray(decodedByteArray, 0, decodedByteArray.length);

        return new HttpRequestResult<>(bitmap);
    }

    @Override
    public HttpRequestResult<Bitmap> resolveImageResource(String uri, GenericImageLoaderAsync genericImageLoaderAsync, int maxWidth) throws IOException, URISyntaxException
    {
        Bitmap bitmap;
        if (uri.startsWith("data:image/svg")) {
            // unescape CR/LF in base64 URI
            String dataUri = AdaptiveBase64Util.ExtractDataFromUri(uri)
                    .replaceAll("%0D", "\r")
                    .replaceAll("%0A", "\n");
            byte[] decodedByteArray = Base64.decode(dataUri, Base64.NO_WRAP);
            String decodedSvgString = new String(decodedByteArray);
            Sharp sharp = Sharp.loadString(decodedSvgString);
            Drawable drawable = sharp.getDrawable();
            bitmap = drawableToBitmap(drawable, maxWidth);
        }
        else
        {
            try
            {
                return genericImageLoaderAsync.loadDataUriImage(uri);
            }
            catch (Exception e)
            {
                return new HttpRequestResult<>(e);
            }
        }

        return new HttpRequestResult<>(bitmap);
    }

    private static Bitmap drawableToBitmap(Drawable drawable, int maxWidth)
    {
        if (drawable instanceof BitmapDrawable){
            return ((BitmapDrawable)drawable).getBitmap();
        }
        else {
            int height = (int)((float)maxWidth * ((float)drawable.getIntrinsicHeight() / (float)drawable.getIntrinsicWidth()));

            Bitmap bitmap;
            try {
                // this can cause OutOfMemoryError
                bitmap = Bitmap.createBitmap(maxWidth, height, Bitmap.Config.ARGB_8888);
            } catch (OutOfMemoryError exception){
                bitmap = Bitmap.createBitmap(0, 0, Bitmap.Config.ARGB_8888);
            }
            Canvas canvas = new Canvas(bitmap);
            drawable.setBounds(0, 0, canvas.getWidth(), canvas.getHeight());
            drawable.draw(canvas);
            return bitmap;
        }
    }
}
